using System.Threading.Tasks;

namespace ActiveMQ.Net
{
    public static class ConnectionExtensions
    {
        public static Task<IConsumer> CreateConsumerAsync(this IConnection connection, string address)
        {
            return connection.CreateConsumerAsync(address, RoutingType.Anycast);
        }
        
        public static Task<IConsumer> CreateConsumerAsync(this IConnection connection, string address, RoutingType routingType, string queue)
        {
            var fullyQualifiedQueueName = CreateFullyQualifiedQueueName(address, queue);
            return connection.CreateConsumerAsync(fullyQualifiedQueueName, routingType);
        }
        
        private static string CreateFullyQualifiedQueueName(string address, string queue)
        {
            return $"{address}::{queue}";
        }
        
        public static Task<IProducer> CreateProducer(this IConnection connection, string address)
        {
            return connection.CreateProducer(address, RoutingType.Anycast);
        }
    }
}