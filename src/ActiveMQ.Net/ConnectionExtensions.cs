using System.Threading;
using System.Threading.Tasks;

namespace ActiveMQ.Net
{
    public static class ConnectionExtensions
    {
        public static Task<IConsumer> CreateConsumerAsync(this IConnection connection, string address)
        {
            return connection.CreateConsumerAsync(address, RoutingType.Anycast, CancellationToken.None);
        }
        
        public static Task<IConsumer> CreateConsumerAsync(this IConnection connection, string address, CancellationToken cancellationToken)
        {
            return connection.CreateConsumerAsync(address, RoutingType.Anycast, cancellationToken);
        }
        
        public static Task<IConsumer> CreateConsumerAsync(this IConnection connection, string address, RoutingType routingType)
        {
            return connection.CreateConsumerAsync(address, routingType, CancellationToken.None);
        }

        public static Task<IConsumer> CreateConsumerAsync(this IConnection connection, string address, RoutingType routingType, string queue)
        {
            return connection.CreateConsumerAsync(address, routingType, queue, CancellationToken.None);
        }

        public static Task<IConsumer> CreateConsumerAsync(this IConnection connection, string address, RoutingType routingType, string queue, CancellationToken cancellationToken)
        {
            var fullyQualifiedQueueName = CreateFullyQualifiedQueueName(address, queue);
            return connection.CreateConsumerAsync(fullyQualifiedQueueName, routingType, cancellationToken);
        }

        private static string CreateFullyQualifiedQueueName(string address, string queue)
        {
            return $"{address}::{queue}";
        }

        public static Task<IProducer> CreateProducer(this IConnection connection, string address)
        {
            return connection.CreateProducerAsync(address, RoutingType.Anycast);
        }
    }
}