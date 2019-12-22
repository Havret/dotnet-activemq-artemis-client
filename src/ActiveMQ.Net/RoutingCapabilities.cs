using Amqp.Types;

namespace ActiveMQ.Net
{
    public static class RoutingCapabilities
    {
        public static readonly Symbol Anycast = new Symbol("queue");
        public static readonly Symbol Multicast = new Symbol("topic");
    }
}