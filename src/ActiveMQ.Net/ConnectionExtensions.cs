using System.Threading;
using System.Threading.Tasks;

namespace ActiveMQ.Net
{
    public static class ConnectionExtensions
    {
        public static Task<IConsumer> CreateConsumerAsync(this IConnection connection, string address, CancellationToken cancellationToken = default)
        {
            var configuration = new ConsumerConfiguration
            {
                Address = address,
                RoutingType = RoutingType.Anycast
            };
            return connection.CreateConsumerAsync(configuration, cancellationToken);
        }

        public static Task<IConsumer> CreateConsumerAsync(this IConnection connection, string address, RoutingType routingType, CancellationToken cancellationToken = default)
        {
            var configuration = new ConsumerConfiguration
            {
                Address = address,
                RoutingType = routingType
            };
            return connection.CreateConsumerAsync(configuration, cancellationToken);
        }

        public static Task<IConsumer> CreateConsumerAsync(this IConnection connection, string address, RoutingType routingType, string queue, CancellationToken cancellationToken = default)
        {
            var configuration = new ConsumerConfiguration
            {
                Address = address,
                RoutingType = routingType,
                Queue = queue
            };
            return connection.CreateConsumerAsync(configuration, cancellationToken);
        }

        public static Task<IProducer> CreateProducerAsync(this IConnection connection, string address, CancellationToken cancellationToken = default)
        {
            var configuration = new ProducerConfiguration
            {
                DefaultAddress = address,
            };
            return connection.CreateProducerAsync(configuration, cancellationToken);
        }

        public static Task<IProducer> CreateProducerAsync(this IConnection connection, string address, RoutingType routingType, CancellationToken cancellationToken = default)
        {
            var configuration = new ProducerConfiguration
            {
                DefaultAddress = address,
                DefaultRoutingType = routingType
            };            
            return connection.CreateProducerAsync(configuration, cancellationToken);
        }
    }
}