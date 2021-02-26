using Amqp;

namespace ActiveMQ.Artemis.Client
{
    internal static class AmqpLinkExtensions
    {
        internal static bool IsDetaching(this Link link)
        {
            return link.LinkState >= LinkState.DetachPipe;
        }
    }
}