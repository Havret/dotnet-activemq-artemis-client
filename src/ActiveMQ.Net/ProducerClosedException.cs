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

        internal static ProducerClosedException BecauseProducerDetached()
        {
            return new ProducerClosedException(ErrorCode.IllegalState, "Producer detached.");
        }
        
        internal static ProducerClosedException FromError(Error error)
        {
            return new ProducerClosedException(error.Condition, error.Description);
        }
    }
}