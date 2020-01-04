using System.Collections.Generic;
using System.Threading.Channels;
using System.Threading.Tasks;
using ActiveMQ.Net.InternalUtilities;
using Amqp;
using Amqp.Framing;

namespace ActiveMQ.Net.AutoRecovering
{
    internal class AutoRecoveringConnection : IConnection
    {
        private readonly string _address;
        private Connection _connection;
        private readonly ChannelReader<ConnectCommand> _reader;
        private readonly ChannelWriter<ConnectCommand> _writer;
        private readonly ConcurrentHashSet<AutoRecoveringProducer> _producers = new ConcurrentHashSet<AutoRecoveringProducer>();

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
                    var connection = await CreateConnection().ConfigureAwait(false);
                    foreach (var producer in _producers.Values)
                    {
                        producer.Recover(connection);
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

        public Task<IConsumer> CreateConsumerAsync(string address, RoutingType routingType)
        {
            return _connection.CreateConsumerAsync(address, routingType);
        }

        public IProducer CreateProducer(string address, RoutingType routingType)
        {
            var autoRecoveringProducer = new AutoRecoveringProducer(address, routingType);
            autoRecoveringProducer.Recover(_connection);
            _producers.Add(autoRecoveringProducer);
            return autoRecoveringProducer;
        }

        public ValueTask DisposeAsync()
        {
            _connection.ConnectionClosed -= OnConnectionClosed;
            return _connection.DisposeAsync();
        }
    }
}