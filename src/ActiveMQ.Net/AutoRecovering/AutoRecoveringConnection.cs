﻿using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ActiveMQ.Net.Builders;
using ActiveMQ.Net.InternalUtilities;
using Microsoft.Extensions.Logging;

namespace ActiveMQ.Net.AutoRecovering
{
    internal class AutoRecoveringConnection : IConnection
    {
        private IConnection _connection;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<AutoRecoveringConnection> _logger;
        private readonly Endpoint _endpoint;
        private readonly ChannelReader<ConnectCommand> _reader;
        private readonly ChannelWriter<ConnectCommand> _writer;
        private readonly ConcurrentHashSet<IRecoverable> _recoverables = new ConcurrentHashSet<IRecoverable>();
        private readonly CancellationTokenSource _recoveryCancellationToken = new CancellationTokenSource();
        private readonly Task _recoveryLoopTask;

        public AutoRecoveringConnection(ILoggerFactory loggerFactory, Endpoint endpoint)
        {
            _logger = loggerFactory.CreateLogger<AutoRecoveringConnection>();
            _loggerFactory = loggerFactory;
            _endpoint = endpoint;

            var channel = Channel.CreateUnbounded<ConnectCommand>();
            _reader = channel.Reader;
            _writer = channel.Writer;

            _recoveryLoopTask = Task.Run(async () =>
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

                            _connection = await CreateConnection().ConfigureAwait(false);

                            foreach (var recoverable in _recoverables.Values)
                            {
                                await recoverable.RecoverAsync(_connection, _recoveryCancellationToken.Token).ConfigureAwait(false);
                                recoverable.Resume();
                            }

                            _connection.ConnectionClosed += OnConnectionClosed;
                            connectCommand.NotifyWaiter();

                            Log.ConnectionEstablished(_logger);
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

        public Task InitAsync()
        {
            var tsc = new TaskCompletionSource<bool>();
            _writer.TryWrite(ConnectCommand.InitialConnect(tsc));
            return tsc.Task;
        }

        // TODO: Change this naive implementation with sth more sophisticated
        private async Task<IConnection> CreateConnection()
        {
            try
            {
                var connectionBuilder = new ConnectionBuilder(_loggerFactory);
                return await connectionBuilder.CreateAsync(_endpoint).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                await Task.Delay(100);
                Log.FailedToCreateConnection(_logger, e);
                return await CreateConnection().ConfigureAwait(false);
            }
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

            private static readonly Action<ILogger, Exception> _connectionEstablished = LoggerMessage.Define(
                LogLevel.Trace,
                0,
                "Connection established.");

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

            public static void ConnectionEstablished(ILogger logger)
            {
                if (logger.IsEnabled(LogLevel.Trace))
                {
                    _connectionEstablished(logger, null);
                }
            }
        }
    }
}