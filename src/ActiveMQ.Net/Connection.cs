using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using Amqp;
using Amqp.Framing;
using Amqp.Types;

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

        public Task<IConsumer> CreateConsumerAsync(string address)
        {
            return CreateConsumerAsync(address, RoutingType.Anycast);
        }

        public Task<IConsumer> CreateConsumerAsync(string address, RoutingType routingType, string queue)
        {
            var fullyQualifiedQueueName = CreateFullyQualifiedQueueName(address, queue);
            return CreateConsumerAsync(fullyQualifiedQueueName, routingType);
        }

        public Task<IConsumer> CreateConsumerAsync(string address, RoutingType routingType)
        {
            var consumerBuilder = new ConsumerBuilder(_session);
            return consumerBuilder.CreateAsync(address, routingType);
        }

        private string CreateFullyQualifiedQueueName(string address, string queue)
        {
            return $"{address}::{queue}";
        }

        public IProducer CreateProducer(string address)
        {
            return CreateProducer(address, RoutingType.Anycast);
        }

        public IProducer CreateProducer(string address, RoutingType routingType)
        {
            var routingCapability = routingType.GetRoutingCapability();
            var senderLink = new SenderLink(_session, Guid.NewGuid().ToString(), new Target
            {
                Address = address,
                Capabilities = new[] { routingCapability }
            }, null);
            return new Producer(senderLink);
        }

        public async ValueTask DisposeAsync()
        {
            await _connection.CloseAsync().ConfigureAwait(false);
        }
    }
}