using System;
using Amqp.Framing;

namespace ActiveMQ.Net.Exceptions
{
    public class MessageSendException : Exception
    {
        public MessageSendException(string condition, string description) : base(description)
        {
            Condition = condition;
        }

        public string Condition { get; }

        internal static MessageSendException FromError(Error error)
        {
            return new MessageSendException(error.Condition, error.Description);
        }

        public static MessageSendException FromMessage(string message)
        {
            return new MessageSendException(null, message);
        }
    }
}