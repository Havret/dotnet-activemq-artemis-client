using System;
using Amqp.Framing;

namespace ActiveMQ.Artemis.Client.Exceptions
{
    public class CreateEndpointException : Exception
    {
        public string Condition { get; }

        internal CreateEndpointException(string condition, string description) : base(description)
        {
            Condition = condition;
        }

        internal CreateEndpointException(string message, Exception exception) : base(message, exception)
        {
        }

        internal static CreateEndpointException FromError(Error error)
        {
            return new CreateEndpointException(error.Condition, error.Description);
        }
    }
}