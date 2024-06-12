using System;
using System.Collections.Generic;
using System.Linq;
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
        private IConnection _connection;
        private readonly ILoggerFactory _loggerFactory;
        private readonly Func<IMessageIdPolicy> _messageIdPolicyFactory;
        private readonly Func<string> _clientIdFactory;
        private readonly SslSettings _sslSettings;
        private readonly TcpSettings _tcpSettings;
        private readonly ILogger<AutoRecoveringConnection> _logger;
        private readonly Endpoint[] _endpoints;
        private readonly ChannelReader<ConnectCommand> _reader;
        private readonly ChannelWriter<ConnectCommand> _writer;
        private readonly ConcurrentHashSet<IRecoverable> _recoverables = new();
        private readonly CancellationTokenSource _recoveryCancellationToken = new();
        private readonly AsyncRetryPolicy<IConnection> _connectionRetryPolicy;
        private readonly Task _recoveryLoopTask;

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
                                try
                                {
                                    await recoverable.RecoverAsync(_connection, _recoveryCancellationToken.Token).ConfigureAwait(false);
                                    recoverable.Resume();
                                }
                                catch (Exception e)
                                {
                                    _recoverables.Remove(recoverable);
                                    await recoverable.TerminateAsync(e).ConfigureAwait(false);
                                }
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

                    foreach (var recoverable in _recoverables.Values)
                    {
                        await recoverable.TerminateAsync(e).ConfigureAwait(false);
                    }

                    ConnectionRecoveryError?.Invoke(this, new ConnectionRecoveryErrorEventArgs(e));
                }
            });
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

        private Task<IConnection> CreateConnection(CancellationToken cancellationToken)
        {
            var ctx = new Context();
            ctx.SetRetryCount(0);
            return _connectionRetryPolicy.ExecuteAsync(async (context, ct) =>
            {
                int retryCount = context.GetRetryCount();
                var endpoint = GetNextEndpoint(retryCount);
                context.SetEndpoint(endpoint);
                var connectionBuilder = new ConnectionBuilder(_loggerFactory, _messageIdPolicyFactory, _clientIdFactory, _sslSettings, _tcpSettings);
                
                Log.TryingToEstablishedConnection(_logger, endpoint, retryCount);
                var connection = await connectionBuilder.CreateAsync(endpoint, ct).ConfigureAwait(false);
                Log.ConnectionEstablished(_logger, endpoint, retryCount);
                
                return connection;
            }, ctx, cancellationToken);
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