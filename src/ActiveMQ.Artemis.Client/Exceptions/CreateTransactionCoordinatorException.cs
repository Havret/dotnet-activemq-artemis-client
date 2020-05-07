using System;
using Amqp.Framing;

namespace ActiveMQ.Artemis.Client.Exceptions
{
    public class CreateTransactionCoordinatorException : Exception
    {
        public string Condition { get; }

        private CreateTransactionCoordinatorException(string condition, string description) : base(description)
        {
            Condition = condition;
        }

        internal static CreateTransactionCoordinatorException FromError(Error error)
        {
            return new CreateTransactionCoordinatorException(error.Condition, error.Description);
        }
    }
}