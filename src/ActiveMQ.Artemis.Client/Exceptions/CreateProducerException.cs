using System;
using Amqp.Framing;

namespace ActiveMQ.Artemis.Client.Exceptions
{
    public class CreateProducerException : Exception
    {
        public string Condition { get; }

        private CreateProducerException(string condition, string description) : base(description)
        {
            Condition = condition;
        }

        internal static CreateProducerException FromError(Error error)
        {
            return new CreateProducerException(error.Condition, error.Description);
        }
    }
}