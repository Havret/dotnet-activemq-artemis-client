using System;
using Amqp.Framing;

namespace ActiveMQ.Net.Exceptions
{
    public class CreateConnectionException : Exception
    {
        public string Condition { get; }

        private CreateConnectionException(string condition, string description) : base(description)
        {
            Condition = condition;
        }
        
        internal CreateConnectionException(string message) : base(message)
        {
        }
        
        internal static CreateConnectionException FromError(Error error)
        {
            return new CreateConnectionException(error.Condition, error.Description);
        }
    }
}