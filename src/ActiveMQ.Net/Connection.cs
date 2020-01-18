using System.Threading.Tasks;
using Amqp;

namespace ActiveMQ.Net
{
    internal class Connection : IConnection
    {
        private readonly Amqp.IConnection _connection;
        private readonly Session _session;

        public Connection(Amqp.IConnection connection, Session session)
        {
            _connection = connection;
            _session = session;
        }

        public bool IsClosed => _connection.IsClosed;

        public Task<IConsumer> CreateConsumerAsync(string address, RoutingType routingType)
        {
            var consumerBuilder = new ConsumerBuilder(_session);
            return consumerBuilder.CreateAsync(address, routingType);
        }

        public Task<IProducer> CreateProducerAsync(string address, RoutingType routingType)
        {
            var producerBuilder = new ProducerBuilder(_session);
            return producerBuilder.CreateAsync(address, routingType);
        }

        public async ValueTask DisposeAsync()
        {
            await _connection.CloseAsync().ConfigureAwait(false);
        }

        public event ClosedCallback ConnectionClosed
        {
            add => _connection.AddClosedCallback(value);
            remove => _connection.Closed -= value;
        }
    }
}