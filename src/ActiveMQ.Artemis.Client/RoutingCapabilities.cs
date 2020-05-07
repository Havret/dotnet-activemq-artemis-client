using Amqp.Types;

namespace ActiveMQ.Artemis.Client
{
    public static class RoutingCapabilities
    {
        public static readonly Symbol Anycast = new Symbol("queue");
        public static readonly Symbol Multicast = new Symbol("topic");
    }
}