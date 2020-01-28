using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
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

        public AutoRecoveringConnection(ILoggerFactory loggerFactory, IEnumerable<Endpoint> endpoints)
        {
            _logger = loggerFactory.CreateLogger<AutoRecoveringConnection>();
            _loggerFactory = loggerFactory;
            _endpoints = endpoints.ToArray();

            var channel = Channel.CreateUnbounded<ConnectCommand>();
            _reader = channel.Reader;
            _writer = channel.Writer;

            _connectionRetryPolicy = CreateConnectionRetryPolicy();

            _recoveryLoopTask = StartRecoveryLoop();
        }

        private AsyncRetryPolicy<IConnection> CreateConnectionRetryPolicy()
        {
            return Policy<IConnection>
                   .Handle<Exception>()
                   .WaitAndRetryForeverAsync((retryAttempt, context) =>
                   {
                       context.SetRetryCount(retryAttempt);
                       return TimeSpan.FromMilliseconds(100);
                   }, (result, _, __) =>
                   {
                       if (result.Exception != null)
                       {
                           Log.FailedToCreateConnection(_logger, result.Exception);
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
                        var connectCommand = await _reader.ReadAsync(_recoveryCancellationToken.Token).ConfigureAwait(false);

                        if (!IsOpened)
                        {
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
                            connectCommand.NotifyWaiter();

                            Log.ConnectionRecovered(_logger);
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
                }
            }, _recoveryCancellationToken.Token);
        }

        private void OnConnectionClosed(object sender, ConnectionClosedEventArgs args)
        {
            if (args.ClosedByPeer)
            {
                Log.ConnectionClosed(_logger, args.Error);
                _writer.TryWrite(ConnectCommand.Empty);
            }

            ConnectionClosed?.Invoke(sender, args);
        }

        public async Task InitAsync()
        {
            _connection = await CreateConnection(CancellationToken.None).ConfigureAwait(false);
            _connection.ConnectionClosed += OnConnectionClosed;
        }

        private Task<IConnection> CreateConnection(CancellationToken cancellationToken)
        {
            var ctx = new Context();
            ctx.SetRetryCount(0);
            return _connectionRetryPolicy.ExecuteAsync((context, ct) =>
            {
                var retryCount = context.GetRetryCount();
                var endpoint = _endpoints[retryCount % _endpoints.Length];
                var connectionBuilder = new ConnectionBuilder(_loggerFactory);
                return connectionBuilder.CreateAsync(endpoint);
            }, ctx, cancellationToken);
        }

        // TODO: Probably should return false only when connection was explicitly closed.
        public bool IsOpened => _connection != null && _connection.IsOpened;

        public async Task<IConsumer> CreateConsumerAsync(string address, RoutingType routingType, CancellationToken cancellationToken)
        {
            var autoRecoveringConsumer = new AutoRecoveringConsumer(_loggerFactory, address, routingType);
            await PrepareRecoverable(autoRecoveringConsumer, cancellationToken).ConfigureAwait(false);
            return autoRecoveringConsumer;
        }

        public async Task<IProducer> CreateProducerAsync(string address, RoutingType routingType, CancellationToken cancellationToken)
        {
            var autoRecoveringProducer = new AutoRecoveringProducer(_loggerFactory, address, routingType);
            await PrepareRecoverable(autoRecoveringProducer, cancellationToken).ConfigureAwait(false);
            return autoRecoveringProducer;
        }

        public event EventHandler<ConnectionClosedEventArgs> ConnectionClosed;

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
            _writer.TryWrite(ConnectCommand.Empty);
        }

        public async ValueTask DisposeAsync()
        {
            _recoveryCancellationToken.Cancel();
            await _recoveryLoopTask.ConfigureAwait(false);
            await _connection.DisposeAsync().ConfigureAwait(false);
            _connection.ConnectionClosed -= OnConnectionClosed;
        }

        private static class Log
        {
            private static readonly Action<ILogger, Exception> _failedToCreateConnection = LoggerMessage.Define(
                LogLevel.Error,
                0,
                "Failed to create connection.");

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

            public static void FailedToCreateConnection(ILogger logger, Exception e)
            {
                if (logger.IsEnabled(LogLevel.Error))
                {
                    _failedToCreateConnection(logger, e);
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