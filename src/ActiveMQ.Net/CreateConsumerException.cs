using System;
using Amqp.Framing;

namespace ActiveMQ.Net
{
    public class CreateConsumerException : Exception
    {
        public string Condition { get; }

        private CreateConsumerException(string condition, string description) : base(description)
        {
            Condition = condition;
        }

        internal static CreateConsumerException FromError(Error error)
        {
            return new CreateConsumerException(error.Condition, error.Description);
        }
    }
}