using System;
using System.Threading.Tasks;
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

        public IConsumer CreateConsumer(string address)
        {
            return CreateConsumer(address, RoutingType.Anycast);
        }

        public IConsumer CreateConsumer(string address, RoutingType routingType)
        {
            var routingCapability = GetRoutingCapability(routingType);

            var receiverLink = new ReceiverLink(_session, Guid.NewGuid().ToString(), new Source()
            {
                Address = address,
                Capabilities = new[] { routingCapability }
            }, null);
            return new Consumer(receiverLink);
        }

        private static Symbol GetRoutingCapability(RoutingType routingType)
        {
            return routingType switch
            {
                RoutingType.Anycast => RoutingCapabilities.Anycast,
                RoutingType.Multicast => RoutingCapabilities.Multicast,
                _ => throw new ArgumentOutOfRangeException(nameof(routingType), $"RoutingType {routingType.ToString()} is not supported.")
            };
        }

        public IConsumer CreateConsumer(string address, RoutingType routingType, ConsumerConfig config)
        {
            throw new NotImplementedException();
        }

        public IProducer CreateProducer(string address, RoutingType routingType)
        {
            var senderLink = new SenderLink(_session, Guid.NewGuid().ToString(), address);
            return new Producer(senderLink);
        }

        public Task CloseAsync()
        {
            return _connection.CloseAsync();
        }

        public async ValueTask DisposeAsync()
        {
            await _connection.CloseAsync().ConfigureAwait(false);
        }
    }
}