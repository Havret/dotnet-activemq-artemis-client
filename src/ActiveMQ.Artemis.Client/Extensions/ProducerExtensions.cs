using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Transactions;

namespace ActiveMQ.Artemis.Client
{
    public static class ProducerExtensions
    {
        public static Task SendAsync(this IProducer producer, Message message, CancellationToken cancellationToken = default)
        {
            return producer.SendAsync(message, null, cancellationToken);
        }

        public static Task SendAsync(this IAnonymousProducer producer, string address, RoutingType routingType, Message message, CancellationToken cancellationToken = default)
        {
            return producer.SendAsync(address, routingType, message, null, cancellationToken);
        }

        public static Task SendAsync(this IAnonymousProducer producer, string address, Message message, CancellationToken cancellationToken = default)
        {
            return producer.SendAsync(address, null, message, null, cancellationToken);
        }

        public static Task SendAsync(this IAnonymousProducer producer, string address, Message message, Transaction transaction, CancellationToken cancellationToken = default)
        {
            return producer.SendAsync(address, null, message, transaction, cancellationToken);
        }
    }
}