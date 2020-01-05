using System.Threading.Channels;
using System.Threading.Tasks;
using ActiveMQ.Net.InternalUtilities;
using Amqp;
using Amqp.Framing;

namespace ActiveMQ.Net.AutoRecovering
{
    internal class AutoRecoveringConnection : IConnection
    {
        private Connection _connection;
        private readonly string _address;
        private readonly ChannelReader<ConnectCommand> _reader;
        private readonly ChannelWriter<ConnectCommand> _writer;
        private readonly ConcurrentHashSet<IRecoverable> _recoverables = new ConcurrentHashSet<IRecoverable>();

        public AutoRecoveringConnection(string address)
        {
            _address = address;

            var channel = Channel.CreateUnbounded<ConnectCommand>();
            _reader = channel.Reader;
            _writer = channel.Writer;

            Task.Run(async () =>
            {
                while (true)
                {
                    var connectCommand = await _reader.ReadAsync().ConfigureAwait(false);
                    var connection = await CreateConnection().ConfigureAwait(false);
                    foreach (var recoverable in _recoverables.Values)
                    {
                        await recoverable.RecoverAsync(connection).ConfigureAwait(false);
                    }

                    _connection = connection;
                    _connection.ConnectionClosed += OnConnectionClosed;
                    connectCommand.NotifyWaiter();
                }
            });
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

        private async Task<Connection> CreateConnection()
        {
            var connectionFactory = new Amqp.ConnectionFactory();
            var connection = await connectionFactory.CreateAsync(new Address(_address)).ConfigureAwait(false);
            var session = new Session(connection);
            return new Connection(connection, session);
        }

        public async Task<IConsumer> CreateConsumerAsync(string address, RoutingType routingType)
        {
            var autoRecoveringConsumer = new AutoRecoveringConsumer(address, routingType);
            await PrepareRecoverable(autoRecoveringConsumer).ConfigureAwait(false);
            return autoRecoveringConsumer;
        }

        public IProducer CreateProducer(string address, RoutingType routingType)
        {
            var autoRecoveringProducer = new AutoRecoveringProducer(address, routingType);
            // TODO: Remove GetAwaiter when CreateProducer changed to async https://github.com/Havret/ActiveMQ.Net/issues/7
            PrepareRecoverable(autoRecoveringProducer).ConfigureAwait(false).GetAwaiter().GetResult();
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

        public ValueTask DisposeAsync()
        {
            _connection.ConnectionClosed -= OnConnectionClosed;
            return _connection.DisposeAsync();
        }
    }
}