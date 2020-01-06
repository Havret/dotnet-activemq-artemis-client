using Amqp;

namespace ActiveMQ.Net
{
    internal static class AmqpLinkExtensions
    {
        internal static bool IsDetaching(this Link link)
        {
            return link.LinkState >= LinkState.DetachPipe;
        }
    }
}