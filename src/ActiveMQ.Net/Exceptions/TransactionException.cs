using System;
using Amqp.Framing;

namespace ActiveMQ.Net.Exceptions
{
    public class TransactionException : Exception
    {
        public string Condition { get; }

        private TransactionException(string condition, string description) : base(description)
        {
            Condition = condition;
        }

        internal static TransactionException FromError(Error error)
        {
            return new TransactionException(error.Condition, error.Description);
        }
    }
}