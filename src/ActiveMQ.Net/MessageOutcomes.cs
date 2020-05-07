using Amqp.Framing;
using Amqp.Transactions;

namespace ActiveMQ.Net
{
    internal static class MessageOutcomes
    {
        public static readonly Accepted Accepted = new Accepted();
        public static readonly Released Released = new Released();
        public static readonly Rejected Rejected = new Rejected();
        public static readonly Declared Declared = new Declared();
    }
}