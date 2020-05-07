using System.Threading.Tasks;

namespace ActiveMQ.Net
{
    public static class ConsumerExtensions
    {
        public static ValueTask AcceptAsync(this IConsumer consumer, Message message)
        {
            return consumer.AcceptAsync(message, null);
        }
    }
}