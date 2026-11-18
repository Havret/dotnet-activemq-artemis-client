using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.AutoRecovering.RecoveryPolicy;
using ActiveMQ.Artemis.Client.Builders;
using ActiveMQ.Artemis.Client.Exceptions;
using ActiveMQ.Artemis.Client.InternalUtilities;
using ActiveMQ.Artemis.Client.MessageIdPolicy;
using Amqp;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace ActiveMQ.Artemis.Client.AutoRecovering
{
    internal class AutoRecoveringConnection : IConnection
    {
        private volatile IConnection _connection;
        private readonly ILoggerFactory _loggerFactory;
        private readonly Func<IMessageIdPolicy> _messageIdPolicyFactory;
        private readonly Func<string> _clientIdFactory;
        private readonly SslSettings _sslSettings;
        private readonly TcpSettings _tcpSettings;
        private readonly ILogger<AutoRecoveringConnection> _logger;
        private readonly Endpoint[] _endpoints;
        private readonly ChannelReader<ConnectionCommand> _reader;
        private readonly ChannelWriter<ConnectionCommand> _writer;

        // Creations taken off the channel during a recovery, touched by the recovery loop only
        private readonly Queue<CreateRecoverableCommand> _deferredCommands = new();

        // The creation being handled, touched by the recovery loop only. When the connection has to be recovered first,
        // it's completed as a part of the recovery.
        private CreateRecoverableCommand _inFlightCreation;

        private readonly ConcurrentHashSet<IRecoverable> _recoverables = new();
        private readonly CancellationTokenSource _recoveryCancellationToken = new();
        private readonly AsyncRetryPolicy<IConnection> _connectionRetryPolicy;
        private readonly Task _recoveryLoopTask;
        private volatile Exception _failureCause;
        private Task _pendingEvents = Task.CompletedTask;

        // Set while an event handler runs, so that disposing the connection from it doesn't wait for the handler itself
        private readonly AsyncLocal<bool> _insideEventHandler = new();

        public AutoRecoveringConnection(ILoggerFactory loggerFactory,
            IEnumerable<Endpoint> endpoints,
            IRecoveryPolicy recoveryPolicy,
            Func<IMessageIdPolicy> messageIdPolicyFactory,
            Func<string> clientIdFactory,
            SslSettings sslSettings,
            TcpSettings tcpSettings)
        {
            _logger = loggerFactory.CreateLogger<AutoRecoveringConnection>();
            _loggerFactory = loggerFactory;
            _messageIdPolicyFactory = messageIdPolicyFactory;
            _clientIdFactory = clientIdFactory;
            _sslSettings = sslSettings;
            _tcpSettings = tcpSettings;
            _endpoints = endpoints.ToArray();

            var channel = Channel.CreateUnbounded<ConnectionCommand>();
            _reader = channel.Reader;
            _writer = channel.Writer;

            _connectionRetryPolicy = CreateConnectionRetryPolicy(recoveryPolicy);

            _recoveryLoopTask = StartRecoveryLoop();
        }

        public Endpoint Endpoint => _connection.Endpoint;

        // TODO: Probably should return false only when connection was explicitly closed.
        public bool IsOpened => _connection != null && _connection.IsOpened;

        private AsyncRetryPolicy<IConnection> CreateConnectionRetryPolicy(IRecoveryPolicy recoveryPolicy)
        {
            return Policy<IConnection>
                   .Handle<Exception>(exception => exception switch
                   {
                       CreateConnectionException { ErrorCode: ErrorCode.UnauthorizedAccess } => false,
                       _ => true
                   })
                   .WaitAndRetryAsync(recoveryPolicy.RetryCount, (retryAttempt, context) =>
                   {
                       context.SetRetryCount(retryAttempt);
                       return recoveryPolicy.GetDelay(retryAttempt);
                   }, (result, _, context) =>
                   {
                       var retryCount = context.GetRetryCount();
                       var endpoint = context.GetEndpoint();
                       if (result.Exception != null)
                       {
                           Log.FailedToEstablishConnection(_logger, endpoint, retryCount, result.Exception);
                       }
                   });
        }

        private Task StartRecoveryLoop()
        {
            return Task.Run(async () =>
            {
                try
                {
                    while (true)
                    {
                        // Commands taken off the channel during a recovery go first, they were queued before the ones still in there
                        var command = _deferredCommands.Count > 0
                            ? _deferredCommands.Dequeue()
                            : await _reader.ReadAsync(_recoveryCancellationToken.Token).ConfigureAwait(false);

                        await HandleCommand(command, _recoveryCancellationToken.Token).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException)
                {
                    // expected when recovery cancellation token is set.
                }
                catch (Exception e)
                {
                    Log.MainRecoveryLoopException(_logger, e);

                    _failureCause = e;
                    _writer.TryComplete();

                    foreach (var recoverable in _recoverables.Values)
                    {
                        await recoverable.TerminateAsync(e).ConfigureAwait(false);
                    }

                    RaiseEvent(() => ConnectionRecoveryError?.Invoke(this, new ConnectionRecoveryErrorEventArgs(e)));
                }
                finally
                {
                    // From now on no command is going to be processed. Reject new ones and fail the pending ones.
                    _writer.TryComplete();
                    _inFlightCreation?.Completion.TrySetException(CreateTerminalException());
                    _inFlightCreation = null;
                    while (_deferredCommands.Count > 0)
                    {
                        _deferredCommands.Dequeue().Completion.TrySetException(CreateTerminalException());
                    }
                    while (_reader.TryRead(out var pending))
                    {
                        if (pending is CreateRecoverableCommand create)
                        {
                            create.Completion.TrySetException(CreateTerminalException());
                        }
                    }
                }
            });
        }

        private Task HandleCommand(ConnectionCommand command, CancellationToken cancellationToken)
        {
            if (command is CreateRecoverableCommand create)
            {
                _inFlightCreation = create;
                return HandleCreateRecoverable(cancellationToken);
            }

            return HandleConnect(cancellationToken);
        }

        private async Task HandleConnect(CancellationToken cancellationToken)
        {
            if (!IsOpened)
            {
                await RecoverConnection(cancellationToken).ConfigureAwait(false);
                return;
            }

            // The connection is fine, but some recoverables may have been suspended because their own links were closed,
            // e.g. detached by the broker. Resuming them as they are would make them fail again straight away.
            var connection = _connection;
            foreach (var recoverable in _recoverables.Values.Where(x => x.IsSuspended))
            {
                try
                {
                    await recoverable.RecoverAsync(connection, cancellationToken).ConfigureAwait(false);
                    recoverable.Resume();
                }
                catch (Exception e) when (IsConnectionLoss(e, connection, CancellationToken.None, cancellationToken))
                {
                    // The connection was lost in the meantime, recover it along with all the recoverables
                    await RecoverConnection(cancellationToken).ConfigureAwait(false);
                    return;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception e)
                {
                    await recoverable.TerminateAsync(e).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Reestablishes the connection and recovers the recoverables on it. The in-flight creation, if any, is completed
        /// as a part of the recovery. Losing the connection before all of that is done counts as a failed attempt
        /// of the recovery policy, the same as failing to connect, so that its delays, endpoint rotation and retry
        /// limit apply.
        /// </summary>
        private async Task RecoverConnection(CancellationToken cancellationToken)
        {
            await DisposeInnerConnection().ConfigureAwait(false);

            foreach (var recoverable in _recoverables.Values)
            {
                recoverable.Suspend();
            }

            var connection = await CreateConnection(cancellationToken, async (established, ct) =>
            {
                var recovered = await RecoverRecoverables(established, ct).ConfigureAwait(false);

                if (_inFlightCreation != null)
                {
                    var connectionLoss = await CreateRecoverable(_inFlightCreation, established, ct).ConfigureAwait(false);
                    if (connectionLoss != null)
                    {
                        ExceptionDispatchInfo.Capture(connectionLoss).Throw();
                    }
                }

                // Resume only once everything is recovered, so that nothing is resumed on a connection that gets lost
                // halfway through, just to be suspended again on the next attempt
                foreach (var recoverable in recovered)
                {
                    recoverable.Resume();
                }
            }).ConfigureAwait(false);

            _connection = connection;

            // If the connection is already closed at this point, subscribing triggers the handler straight away
            connection.ConnectionClosed += OnConnectionClosed;

            Log.ConnectionRecovered(_logger);

            RaiseEvent(() => ConnectionRecovered?.Invoke(this, new ConnectionRecoveredEventArgs(connection.Endpoint)));
        }

        /// <returns>The recoverables that were recovered. They are left suspended, it's up to the caller to resume them.</returns>
        /// <remarks>Throws when the connection is lost, so that the recovery attempt fails as a whole.</remarks>
        private async Task<List<IRecoverable>> RecoverRecoverables(IConnection connection, CancellationToken cancellationToken)
        {
            var recovered = new List<IRecoverable>();
            foreach (var recoverable in _recoverables.Values)
            {
                try
                {
                    await recoverable.RecoverAsync(connection, cancellationToken).ConfigureAwait(false);
                    recovered.Add(recoverable);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception e) when (!IsConnectionLoss(e, connection, CancellationToken.None, cancellationToken))
                {
                    await recoverable.TerminateAsync(e).ConfigureAwait(false);
                }
            }

            return recovered;
        }

        /// <summary>
        /// Handles <see cref="_inFlightCreation"/>.
        /// </summary>
        private async Task HandleCreateRecoverable(CancellationToken loopCancellationToken)
        {
            if (!IsOpened || await CreateRecoverable(_inFlightCreation, _connection, loopCancellationToken).ConfigureAwait(false) != null)
            {
                // Either there is no connection, or it was lost while the recoverable was being created. Create the
                // recoverable as a part of the recovery, so that the connection being lost over and over again is
                // subject to the recovery policy.
                await RecoverConnection(loopCancellationToken).ConfigureAwait(false);
            }

            _inFlightCreation = null;
        }

        /// <summary>
        /// Creates the recoverable requested by the command on the connection and completes the command.
        /// </summary>
        /// <returns>The exception when the connection was lost before the recoverable was created, otherwise null.</returns>
        private async Task<Exception> CreateRecoverable(CreateRecoverableCommand command, IConnection connection, CancellationToken loopCancellationToken)
        {
            var recoverable = command.Recoverable;

            // The caller gave up while the command was waiting
            if (command.Completion.Task.IsCompleted)
            {
                return null;
            }

            try
            {
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(command.CancellationToken, loopCancellationToken);
                await recoverable.RecoverAsync(connection, linked.Token).ConfigureAwait(false);
            }
            catch (Exception e) when (IsConnectionLoss(e, connection, command.CancellationToken, loopCancellationToken))
            {
                return e;
            }
            catch (OperationCanceledException) when (loopCancellationToken.IsCancellationRequested)
            {
                // The connection is being disposed, the loop takes care of failing the command
                throw;
            }
            catch (Exception e)
            {
                command.Completion.TrySetException(e);
                return null;
            }

            // Register before completing, so that a caller that disposes the recoverable straight away is tracked properly
            _recoverables.Add(recoverable);
            recoverable.Closed += OnRecoverableClosed;
            recoverable.RecoveryRequested += OnRecoveryRequested;

            if (!command.Completion.TrySetResult(true))
            {
                // The caller gave up in the meantime, nobody is going to use the recoverable
                await DisposeAbandonedRecoverable(recoverable).ConfigureAwait(false);
            }

            return null;
        }

        /// <summary>
        /// Best effort, a failure must not take the loop, and so the recovery of every other recoverable, down.
        /// </summary>
        private async Task DisposeAbandonedRecoverable(IRecoverable recoverable)
        {
            try
            {
                await recoverable.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Log.FailedToDisposeAbandonedRecoverable(_logger, e);
            }
            finally
            {
                // A failed disposal may not have raised Closed, make sure the recoverable is no longer recovered
                OnRecoverableClosed(recoverable);
            }
        }

        /// <summary>
        /// Takes the commands queued while the loop is busy with a recovery. Creations are kept to be handled once
        /// the recovery is done, unless their callers have already given up, so that they don't pile up during a long
        /// outage. Connect commands are dropped, the recovery in progress takes care of them.
        /// Must be called from the recovery loop only.
        /// </summary>
        private void DeferQueuedCommands()
        {
            for (var i = _deferredCommands.Count; i > 0; i--)
            {
                var deferred = _deferredCommands.Dequeue();
                if (!deferred.Completion.Task.IsCompleted)
                {
                    _deferredCommands.Enqueue(deferred);
                }
            }

            while (_reader.TryRead(out var command))
            {
                if (command is CreateRecoverableCommand create && !create.Completion.Task.IsCompleted)
                {
                    _deferredCommands.Enqueue(create);
                }
            }
        }

        private static bool IsConnectionLoss(Exception exception, IConnection connection, CancellationToken callerCancellationToken, CancellationToken loopCancellationToken)
        {
            if (connection.IsOpened || callerCancellationToken.IsCancellationRequested || loopCancellationToken.IsCancellationRequested)
            {
                return false;
            }

            // Whatever the failure, the connection is gone, unless the broker explicitly denied access, which retrying won't help
            return exception is not ActiveMQArtemisClientException { ErrorCode: ErrorCode.UnauthorizedAccess };
        }

        private Exception CreateTerminalException()
        {
            return _failureCause != null
                ? new ConnectionClosedException("The Connection was closed due to an unrecoverable error.", _failureCause)
                : new ObjectDisposedException(nameof(Connection));
        }

        /// <summary>
        /// Raises an event outside of the recovery loop, preserving the order of events. Handlers may call back
        /// into the connection (e.g. create a producer and block on it) which would deadlock on the loop.
        /// Must be called from the recovery loop only.
        /// </summary>
        private void RaiseEvent(Action raise)
        {
            _pendingEvents = _pendingEvents.ContinueWith(_ =>
            {
                _insideEventHandler.Value = true;
                try
                {
                    raise();
                }
                catch (Exception e)
                {
                    // Must not take the recovery down, as it would if the handler was called from the loop
                    Log.EventHandlerException(_logger, e);
                }
                finally
                {
                    _insideEventHandler.Value = false;
                }
            }, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
        }

        private void OnConnectionClosed(object sender, ConnectionClosedEventArgs args)
        {
            if (args.ClosedByPeer)
            {
                Log.ConnectionClosedByPeer(_logger, args.Error);
                _writer.TryWrite(ConnectCommand.Instance);
            }
            else
            {
                Log.ConnectionClosed(_logger);
            }

            ConnectionClosed?.Invoke(sender, args);
        }

        public async Task InitAsync(CancellationToken cancellationToken)
        {
            _connection = await CreateConnection(cancellationToken).ConfigureAwait(false);
            _connection.ConnectionClosed += OnConnectionClosed;
        }

        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="recover">
        /// When set, the connection is being recovered by the loop, and this is called on every established connection
        /// to finish the recovery on it. If it throws, the attempt fails and the connection is disposed.
        /// </param>
        private Task<IConnection> CreateConnection(CancellationToken cancellationToken, Func<IConnection, CancellationToken, Task> recover = null)
        {
            var ctx = new Context();
            ctx.SetRetryCount(0);
            return _connectionRetryPolicy.ExecuteAsync(async (context, ct) =>
            {
                if (recover != null)
                {
                    DeferQueuedCommands();
                }

                int retryCount = context.GetRetryCount();
                var endpoint = GetNextEndpoint(retryCount);
                context.SetEndpoint(endpoint);
                var connectionBuilder = new ConnectionBuilder(_loggerFactory, _messageIdPolicyFactory, _clientIdFactory, _sslSettings, _tcpSettings);

                Log.TryingToEstablishedConnection(_logger, endpoint, retryCount + 1);
                var connection = await connectionBuilder.CreateAsync(endpoint, ct).ConfigureAwait(false);
                Log.ConnectionEstablished(_logger, endpoint, retryCount + 1);

                if (recover != null)
                {
                    try
                    {
                        await recover(connection, ct).ConfigureAwait(false);
                    }
                    catch (Exception)
                    {
                        if (!ct.IsCancellationRequested)
                        {
                            Log.ConnectionLostDuringRecovery(_logger);
                        }

                        await DisposeConnectionSafe(connection).ConfigureAwait(false);
                        throw;
                    }
                }

                return connection;
            }, ctx, cancellationToken);
        }

        private static async Task DisposeConnectionSafe(IConnection connection)
        {
            try
            {
                await connection.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        public async Task<ITopologyManager> CreateTopologyManagerAsync(CancellationToken cancellationToken = default)
        {
            var configuration = new RequestReplyClientConfiguration
            {
                Address = "activemq.management"
            };
            var rpcClient = await CreateRequestReplyClientAsync(configuration, cancellationToken).ConfigureAwait(false);
            return new TopologyManager(configuration.Address, rpcClient);
        }

        public async Task<IConsumer> CreateConsumerAsync(ConsumerConfiguration configuration, CancellationToken cancellationToken)
        {
            var autoRecoveringConsumer = new AutoRecoveringConsumer(_loggerFactory, configuration);
            await PrepareRecoverable(autoRecoveringConsumer, cancellationToken).ConfigureAwait(false);
            return autoRecoveringConsumer;
        }

        public async Task<IProducer> CreateProducerAsync(ProducerConfiguration configuration, CancellationToken cancellationToken)
        {
            var autoRecoveringProducer = new AutoRecoveringProducer(_loggerFactory, configuration);
            await PrepareRecoverable(autoRecoveringProducer, cancellationToken).ConfigureAwait(false);
            return autoRecoveringProducer;
        }

        public async Task<IAnonymousProducer> CreateAnonymousProducerAsync(AnonymousProducerConfiguration configuration, CancellationToken cancellationToken = default)
        {
            var autoRecoveringAnonymousProducer = new AutoRecoveringAnonymousProducer(_loggerFactory, configuration);
            await PrepareRecoverable(autoRecoveringAnonymousProducer, cancellationToken).ConfigureAwait(false);
            return autoRecoveringAnonymousProducer;
        }

        public async Task<IRequestReplyClient> CreateRequestReplyClientAsync(RequestReplyClientConfiguration configuration, CancellationToken cancellationToken = default)
        {
            var autoRecoveringRpcClient = new AutoRecoveringRequestReplyClient(_loggerFactory, configuration);
            await PrepareRecoverable(autoRecoveringRpcClient, cancellationToken).ConfigureAwait(false);
            return autoRecoveringRpcClient;
        }

        public event EventHandler<ConnectionClosedEventArgs> ConnectionClosed;
        public event EventHandler<ConnectionRecoveredEventArgs> ConnectionRecovered;
        public event EventHandler<ConnectionRecoveryErrorEventArgs> ConnectionRecoveryError;

        private async Task PrepareRecoverable(IRecoverable recoverable, CancellationToken cancellationToken)
        {
            var command = new CreateRecoverableCommand(recoverable, cancellationToken);
            if (!_writer.TryWrite(command))
            {
                throw CreateTerminalException();
            }

            // The command may be still queued behind a recovery, the caller must be able to walk away
            using var _ = cancellationToken.Register(() => command.Completion.TrySetCanceled(cancellationToken));
            await command.Completion.Task.ConfigureAwait(false);
        }

        private void OnRecoverableClosed(IRecoverable recoverable)
        {
            recoverable.Closed -= OnRecoverableClosed;
            recoverable.RecoveryRequested -= OnRecoveryRequested;
            _recoverables.Remove(recoverable);
        }

        private void OnRecoveryRequested()
        {
            _writer.TryWrite(ConnectCommand.Instance);
        }

        public async ValueTask DisposeAsync()
        {
            _recoveryCancellationToken.Cancel();
            await _recoveryLoopTask.ConfigureAwait(false);

            // Let the handlers of the events raised so far finish before the connection is gone. Unless disposing from
            // one of them, which would wait for itself.
            if (!_insideEventHandler.Value)
            {
                await _pendingEvents.ConfigureAwait(false);
            }
            await Task.WhenAll(_recoverables.Values.Select(async x => await x.DisposeAsync().ConfigureAwait(false))).ConfigureAwait(false);
            await DisposeInnerConnection().ConfigureAwait(false);
        }

        private async Task DisposeInnerConnection()
        {
            _connection.ConnectionClosed -= OnConnectionClosed;
            try
            {
                await _connection.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private Endpoint GetNextEndpoint(int retryCount)
        {
            return _endpoints[retryCount % _endpoints.Length];
        }

        private static class Log
        {
            private static readonly Action<ILogger, string, int, Exception> _connectionEstablished = LoggerMessage.Define<string, int>(
                LogLevel.Information,
                0,
                "Connection to {endpoint} established. Attempt: {attempt}.");

            private static readonly Action<ILogger, string, int, Exception> _failedToEstablishConnection = LoggerMessage.Define<string, int>(
                LogLevel.Warning,
                0,
                "Failed to establish connection to {endpoint}. Attempt: {attempt}.");

            private static readonly Action<ILogger, Exception> _mainRecoveryLoopException = LoggerMessage.Define(
                LogLevel.Error,
                0,
                "Main recovery loop threw unexpected exception.");

            private static readonly Action<ILogger, string, Exception> _connectionClosedByPeer = LoggerMessage.Define<string>(
                LogLevel.Warning,
                0,
                "Connection closed due to {error}. Reconnect scheduled.");
            
            private static readonly Action<ILogger, Exception> _connectionClosed = LoggerMessage.Define(
                LogLevel.Information,
                0,
                "Connection closed. Reconnect won't be scheduled.");            

            private static readonly Action<ILogger, Exception> _connectionRecovered = LoggerMessage.Define(
                LogLevel.Information,
                0,
                "Connection recovered.");

            private static readonly Action<ILogger, Exception> _connectionLostDuringRecovery = LoggerMessage.Define(
                LogLevel.Warning,
                0,
                "Connection lost while recovering producers and consumers.");

            private static readonly Action<ILogger, Exception> _eventHandlerException = LoggerMessage.Define(
                LogLevel.Error,
                0,
                "Connection event handler threw an exception.");

            private static readonly Action<ILogger, Exception> _failedToDisposeAbandonedRecoverable = LoggerMessage.Define(
                LogLevel.Warning,
                0,
                "Failed to dispose a producer or consumer whose creation was cancelled.");

            private static readonly Action<ILogger, string, int, Exception> _tryingToEstablishedConnection = LoggerMessage.Define<string, int>(
                LogLevel.Information,
                0,
                "Trying to establish connection to {endpoint}. Attempt: {attempt}.");

            public static void ConnectionEstablished(ILogger logger, Endpoint endpoint, int attempt)
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    _connectionEstablished(logger, endpoint.ToString(), attempt, null);
                }
            }

            public static void FailedToEstablishConnection(ILogger logger, Endpoint endpoint, int attempt, Exception e)
            {
                if (logger.IsEnabled(LogLevel.Warning))
                {
                    _failedToEstablishConnection(logger, endpoint.ToString(), attempt, e);
                }
            }

            public static void MainRecoveryLoopException(ILogger logger, Exception e)
            {
                if (logger.IsEnabled(LogLevel.Error))
                {
                    _mainRecoveryLoopException(logger, e);
                }
            }

            public static void ConnectionClosedByPeer(ILogger logger, string error)
            {
                if (logger.IsEnabled(LogLevel.Warning))
                {
                    _connectionClosedByPeer(logger, error, null);
                }
            }
            
            public static void ConnectionClosed(ILogger logger)
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    _connectionClosed(logger, null);
                }
            }

            public static void ConnectionLostDuringRecovery(ILogger logger)
            {
                if (logger.IsEnabled(LogLevel.Warning))
                {
                    _connectionLostDuringRecovery(logger, null);
                }
            }

            public static void FailedToDisposeAbandonedRecoverable(ILogger logger, Exception e)
            {
                if (logger.IsEnabled(LogLevel.Warning))
                {
                    _failedToDisposeAbandonedRecoverable(logger, e);
                }
            }

            public static void EventHandlerException(ILogger logger, Exception e)
            {
                if (logger.IsEnabled(LogLevel.Error))
                {
                    _eventHandlerException(logger, e);
                }
            }

            public static void ConnectionRecovered(ILogger logger)
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    _connectionRecovered(logger, null);
                }
            }
            
            public static void TryingToEstablishedConnection(ILogger logger, Endpoint endpoint, int attempt)
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    _tryingToEstablishedConnection(logger, endpoint.ToString(), attempt, null);
                }
            }
        }
    }
}