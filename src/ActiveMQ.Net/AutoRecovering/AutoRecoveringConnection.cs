using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ActiveMQ.Net.InternalUtilities;
using Amqp;
using Amqp.Framing;
using Microsoft.Extensions.Logging;

namespace ActiveMQ.Net.AutoRecovering
{
    internal class AutoRecoveringConnection : IConnection
    {
        private Connection _connection;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<AutoRecoveringConnection> _logger;
        private readonly string _address;
        private readonly ChannelReader<ConnectCommand> _reader;
        private readonly ChannelWriter<ConnectCommand> _writer;
        private readonly ConcurrentHashSet<IRecoverable> _recoverables = new ConcurrentHashSet<IRecoverable>();
        private readonly CancellationTokenSource _recoveryCancellationToken = new CancellationTokenSource();
        private readonly Task _recoveryLoopTask;

        public AutoRecoveringConnection(ILoggerFactory loggerFactory, string address)
        {
            _logger = loggerFactory.CreateLogger<AutoRecoveringConnection>();
            _loggerFactory = loggerFactory;
            _address = address;

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

                        foreach (var recoverable in _recoverables.Values)
                        {
                            recoverable.Suspend();
                        }
                        
                        _connection = await CreateConnection().ConfigureAwait(false);
                        
                        foreach (var recoverable in _recoverables.Values)
                        {
                            await recoverable.RecoverAsync(_connection).ConfigureAwait(false);
                        }

                        _connection.ConnectionClosed += OnConnectionClosed;
                        connectCommand.NotifyWaiter();
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

        private void OnConnectionClosed(IAmqpObject sender, Error error)
        {
            _writer.TryWrite(ConnectCommand.Empty);
        }

        public Task InitAsync()
        {
            var tsc = new TaskCompletionSource<bool>();
            _writer.TryWrite(ConnectCommand.InitialConnect(tsc));
            return tsc.Task;
        }

        // TODO: Change this naive implementation with sth more sophisticated
        private async Task<Connection> CreateConnection()
        {
            try
            {
                var connectionFactory = new Amqp.ConnectionFactory();
                var connection = await connectionFactory.CreateAsync(new Address(_address)).ConfigureAwait(false);
                var session = new Session(connection);
                return new Connection(connection, session);
            }
            catch (Exception e)
            {
                await Task.Delay(100);
                Log.FailedToCreateConnection(_logger, e);
                return await CreateConnection().ConfigureAwait(false);
            }
        }

        public async Task<IConsumer> CreateConsumerAsync(string address, RoutingType routingType)
        {
            var autoRecoveringConsumer = new AutoRecoveringConsumer(_loggerFactory, address, routingType);
            await PrepareRecoverable(autoRecoveringConsumer).ConfigureAwait(false);
            return autoRecoveringConsumer;
        }

        public async Task<IProducer> CreateProducer(string address, RoutingType routingType)
        {
            var autoRecoveringProducer = new AutoRecoveringProducer(_loggerFactory, address, routingType);
            await PrepareRecoverable(autoRecoveringProducer).ConfigureAwait(false);
            return autoRecoveringProducer;
        }

        private async Task PrepareRecoverable(IRecoverable recoverable)
        {
            await recoverable.RecoverAsync(_connection).ConfigureAwait(false);
            _recoverables.Add(recoverable);
            recoverable.Closed += OnRecoverableClosed;
        }

        private void OnRecoverableClosed(IRecoverable recoverable)
        {
            recoverable.Closed -= OnRecoverableClosed;
            _recoverables.Remove(recoverable);
        }

        public async ValueTask DisposeAsync()
        {
            _connection.ConnectionClosed -= OnConnectionClosed;
            _recoveryCancellationToken.Cancel();
            await _recoveryLoopTask.ConfigureAwait(false);
            await _connection.DisposeAsync().ConfigureAwait(false);
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
        }
    }
}