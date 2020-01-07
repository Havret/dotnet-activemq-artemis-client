using Amqp.Framing;

namespace ActiveMQ.Net
{
    internal static class MessageOutcomes
    {
        public static readonly Accepted Accepted = new Accepted();
        public static readonly Released Released = new Released();
        public static readonly Rejected Rejected = new Rejected();
    }
}