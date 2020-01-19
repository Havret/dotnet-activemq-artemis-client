using System;
using Amqp;
using Amqp.Framing;

namespace ActiveMQ.Net
{
    public class ProducerClosedException : Exception
    {
        public string Condition { get; }

        private ProducerClosedException(string condition, string description) : base(description)
        {
            Condition = condition;
        }

        private ProducerClosedException(string message, Exception innerException) : base(message, innerException)
        {
        }

        internal static ProducerClosedException BecauseProducerDetached()
        {
            return new ProducerClosedException(ErrorCode.IllegalState, "Producer detached.");
        }
        
        internal static ProducerClosedException FromError(Error error)
        {
            return new ProducerClosedException(error.Condition, error.Description);
        }
        
        internal static ProducerClosedException FromException(Exception innerException)
        {
            return new ProducerClosedException(innerException.Message, innerException);
        }
    }
}