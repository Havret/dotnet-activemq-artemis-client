using System;
using Amqp.Framing;

namespace ActiveMQ.Net
{
    public class CannotCreateConsumerException : Exception
    {
        public string Condition { get; }

        private CannotCreateConsumerException(string condition, string description) : base(description)
        {
            Condition = condition;
        }

        internal static CannotCreateConsumerException FromError(Error error)
        {
            return new CannotCreateConsumerException(error.Condition, error.Description);
        }
    }
}