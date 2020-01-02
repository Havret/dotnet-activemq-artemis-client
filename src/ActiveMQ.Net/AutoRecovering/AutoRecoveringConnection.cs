using System.Threading.Channels;
using System.Threading.Tasks;
using Amqp;

namespace ActiveMQ.Net.AutoRecovering
{
    internal class AutoRecoveringConnection : IConnection
    {
        private readonly string _address;
        private Connection _connection;
        private readonly ChannelReader<ConnectCommand> _reader;
        private readonly ChannelWriter<ConnectCommand> _writer;

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
                    var connectCommand = await _reader.ReadAsync();
                    _connection = await CreateConnection().ConfigureAwait(false);
                    _connection.ConnectionClosed += (sender, args) => _writer.TryWrite(ConnectCommand.Empty);
                    connectCommand.NotifyWaiter();
                }
            });
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

        public Task<IConsumer> CreateConsumerAsync(string address)
        {
            return _connection.CreateConsumerAsync(address);
        }

        public Task<IConsumer> CreateConsumerAsync(string address, RoutingType routingType)
        {
            return _connection.CreateConsumerAsync(address, routingType);
        }

        public Task<IConsumer> CreateConsumerAsync(string address, RoutingType routingType, string queue)
        {
            return _connection.CreateConsumerAsync(address, routingType, queue);
        }

        public IProducer CreateProducer(string address)
        {
            return _connection.CreateProducer(address);
        }

        public IProducer CreateProducer(string address, RoutingType routingType)
        {
            return _connection.CreateProducer(address, routingType);
        }

        public ValueTask DisposeAsync()
        {
            return _connection.DisposeAsync();
        }
    }
}