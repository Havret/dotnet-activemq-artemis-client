using System.Threading;
using System.Threading.Tasks;

namespace ActiveMQ.Net
{
    public static class ProducerExtensions
    {
        public static Task SendAsync(this IProducer producer, Message message, CancellationToken cancellationToken = default)
        {
            return producer.SendAsync(message, null, cancellationToken);
        }

        public static Task SendAsync(this IAnonymousProducer producer, string address, AddressRoutingType routingType, Message message, CancellationToken cancellationToken = default)
        {
            return producer.SendAsync(address, routingType, message, null, cancellationToken);
        }
    }
}