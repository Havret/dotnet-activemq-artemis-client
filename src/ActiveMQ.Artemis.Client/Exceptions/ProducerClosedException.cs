using System;

namespace ActiveMQ.Artemis.Client.Exceptions
{
    public class ProducerClosedException : ActiveMQArtemisClientException
    {
        public ProducerClosedException(string message) : base(message)
        {
        }

        public ProducerClosedException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public ProducerClosedException(string message, string errorCode, Exception innerException) : base(message, errorCode, innerException)
        {
        }
    }
}