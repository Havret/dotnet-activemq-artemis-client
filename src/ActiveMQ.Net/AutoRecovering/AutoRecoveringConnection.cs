using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ActiveMQ.Net.AutoRecovering.RecoveryPolicy;
using ActiveMQ.Net.Builders;
using ActiveMQ.Net.InternalUtilities;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace ActiveMQ.Net.AutoRecovering
{
    internal class AutoRecoveringConnection : IConnection
    {
        private IConnection _connection;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<AutoRecoveringConnection> _logger;
        private readonly Endpoint[] _endpoints;
        private readonly ChannelReader<ConnectCommand> _reader;
        private readonly ChannelWriter<ConnectCommand> _writer;
        private readonly ConcurrentHashSet<IRecoverable> _recoverables = new ConcurrentHashSet<IRecoverable>();
        private readonly CancellationTokenSource _recoveryCancellationToken = new CancellationTokenSource();
        private readonly AsyncRetryPolicy<IConnection> _connectionRetryPolicy;
        private readonly Task _recoveryLoopTask;

        public AutoRecoveringConnection(ILoggerFactory loggerFactory, IEnumerable<Endpoint> endpoints, IRecoveryPolicy recoveryPolicy)
        {
            _logger = loggerFactory.CreateLogger<AutoRecoveringConnection>();
            _loggerFactory = loggerFactory;
            _endpoints = endpoints.ToArray();

            var channel = Channel.CreateUnbounded<ConnectCommand>();
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
                   .Handle<Exception>()
                   .WaitAndRetryAsync(recoveryPolicy.RetryCount, (retryAttempt, context) =>
                   {
                       context.SetRetryCount(retryAttempt);
                       return recoveryPolicy.GetDelay(retryAttempt);
                   }, (result, _, context) =>
                   {
                       var retryCount = context.GetRetryCount();
                       var endpoint = GetCurrentEndpoint(context);
                       if (result.Exception != null)
                       {
                           Log.FailedToEstablishConnection(_logger, endpoint, retryCount, result.Exception);
                       }
                       else
                       {
                           Log.ConnectionEstablished(_logger, endpoint, retryCount);
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
                        await _reader.ReadAsync(_recoveryCancellationToken.Token).ConfigureAwait(false);

                        if (!IsOpened)
                        {
                            await DisposeInnerConnection().ConfigureAwait(false);
                            
                            foreach (var recoverable in _recoverables.Values)
                            {
                                recoverable.Suspend();
                            }

                            _connection = await CreateConnection(_recoveryCancellationToken.Token).ConfigureAwait(false);

                            foreach (var recoverable in _recoverables.Values)
                            {
                                await recoverable.RecoverAsync(_connection, _recoveryCancellationToken.Token).ConfigureAwait(false);
                                recoverable.Resume();
                            }

                            _connection.ConnectionClosed += OnConnectionClosed;

                            Log.ConnectionRecovered(_logger);
                            
                            ConnectionRecovered?.Invoke(this, new ConnectionRecoveredEventArgs(_connection.Endpoint));
                        }
                        else
                        {
                            // If the connection is already opened it means that there may be some suspended recoverables that need to be resumed
                            foreach (var recoverable in _recoverables.Values)
                            {
                                recoverable.Resume();
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // expected when recovery cancellation token is set.
                }
                catch (Exception e)
                {
                    Log.MainRecoveryLoopException(_logger, e);
                    ConnectionRecoveryError?.Invoke(this, new ConnectionRecoveryErrorEventArgs(e));
                }
            });
        }

        private void OnConnectionClosed(object sender, ConnectionClosedEventArgs args)
        {
            if (args.ClosedByPeer)
            {
                Log.ConnectionClosed(_logger, args.Error);
                _writer.TryWrite(ConnectCommand.Instance);
            }

            ConnectionClosed?.Invoke(sender, args);
        }

        public async Task InitAsync(CancellationToken cancellationToken)
        {
            _connection = await CreateConnection(cancellationToken).ConfigureAwait(false);
            _connection.ConnectionClosed += OnConnectionClosed;
        }

        private Task<IConnection> CreateConnection(CancellationToken cancellationToken)
        {
            var ctx = new Context();
            ctx.SetRetryCount(0);
            return _connectionRetryPolicy.ExecuteAsync((context, ct) =>
            {
                var endpoint = GetCurrentEndpoint(context);
                var connectionBuilder = new ConnectionBuilder(_loggerFactory);
                return connectionBuilder.CreateAsync(endpoint, ct);
            }, ctx, cancellationToken);
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

        public async Task<IAnonymousProducer> CreateAnonymousProducer(AnonymousProducerConfiguration configuration, CancellationToken cancellationToken = default)
        {
            var autoRecoveringAnonymousProducer = new AutoRecoveringAnonymousProducer(_loggerFactory, configuration);
            await PrepareRecoverable(autoRecoveringAnonymousProducer, cancellationToken);
            return autoRecoveringAnonymousProducer;
        }

        public event EventHandler<ConnectionClosedEventArgs> ConnectionClosed;
        
        public event EventHandler<ConnectionRecoveredEventArgs> ConnectionRecovered;
        public event EventHandler<ConnectionRecoveryErrorEventArgs> ConnectionRecoveryError;

        private async Task PrepareRecoverable(IRecoverable recoverable, CancellationToken cancellationToken)
        {
            await recoverable.RecoverAsync(_connection, cancellationToken).ConfigureAwait(false);
            _recoverables.Add(recoverable);
            recoverable.Closed += OnRecoverableClosed;
            recoverable.RecoveryRequested += OnRecoveryRequested;
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
            await DisposeInnerConnection();
        }

        private async Task DisposeInnerConnection()
        {
            await _connection.DisposeAsync().ConfigureAwait(false);
            _connection.ConnectionClosed -= OnConnectionClosed;
        }

        private Endpoint GetCurrentEndpoint(Context context)
        {
            int retryCount = context.GetRetryCount();
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

            private static readonly Action<ILogger, string, Exception> _connectionClosed = LoggerMessage.Define<string>(
                LogLevel.Warning,
                0,
                "Connection closed due to {error}. Reconnect scheduled.");

            private static readonly Action<ILogger, Exception> _connectionRecovered = LoggerMessage.Define(
                LogLevel.Information,
                0,
                "Connection recovered.");

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

            public static void ConnectionClosed(ILogger logger, string error)
            {
                if (logger.IsEnabled(LogLevel.Warning))
                {
                    _connectionClosed(logger, error, null);
                }
            }

            public static void ConnectionRecovered(ILogger logger)
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    _connectionRecovered(logger, null);
                }
            }
        }
    }
}