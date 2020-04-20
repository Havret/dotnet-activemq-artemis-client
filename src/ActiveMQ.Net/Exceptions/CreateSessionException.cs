using System;
using Amqp.Framing;

namespace ActiveMQ.Net.Exceptions
{
    public class CreateSessionException : Exception
    {
        public string Condition { get; }

        private CreateSessionException(string condition, string description) : base(description)
        {
            Condition = condition;
        }

        internal static CreateSessionException FromError(Error error)
        {
            return new CreateSessionException(error.Condition, error.Description);
        }
    }
}