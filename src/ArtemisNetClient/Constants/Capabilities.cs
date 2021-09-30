using Amqp.Types;

namespace ActiveMQ.Artemis.Client
{
    internal static class Capabilities
    {
        public static readonly Symbol Anycast = new("queue");
        public static readonly Symbol Multicast = new("topic");
        public static readonly Symbol Shared = new("shared");
        public static readonly Symbol Global = new("global");
    }
}