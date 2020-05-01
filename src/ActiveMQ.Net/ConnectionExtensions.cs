using System.Threading;
using System.Threading.Tasks;

namespace ActiveMQ.Net
{
    public static class ConnectionExtensions
    {
        public static Task<IConsumer> CreateConsumerAsync(this IConnection connection, string address, QueueRoutingType routingType, CancellationToken cancellationToken = default)
        {
            var configuration = new ConsumerConfiguration
            {
                Address = address,
                RoutingType = routingType
            };
            return connection.CreateConsumerAsync(configuration, cancellationToken);
        }

        public static Task<IConsumer> CreateConsumerAsync(this IConnection connection, string address, QueueRoutingType routingType, string queue, CancellationToken cancellationToken = default)
        {
            var configuration = new ConsumerConfiguration
            {
                Address = address,
                RoutingType = routingType,
                Queue = queue
            };
            return connection.CreateConsumerAsync(configuration, cancellationToken);
        }

        public static Task<IProducer> CreateProducerAsync(this IConnection connection, string address, AddressRoutingType routingType, CancellationToken cancellationToken = default)
        {
            var configuration = new ProducerConfiguration
            {
                Address = address,
                RoutingType = routingType
            };            
            return connection.CreateProducerAsync(configuration, cancellationToken);
        }

        public static Task<IAnonymousProducer> CreateAnonymousProducer(this IConnection connection, CancellationToken cancellationToken = default)
        {
            var configuration = new AnonymousProducerConfiguration();
            return connection.CreateAnonymousProducer(configuration, cancellationToken);
        }
    }
}