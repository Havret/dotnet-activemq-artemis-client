using System;

namespace ActiveMQ.Artemis.Client.Exceptions
{
    public class ProducerClosedException : ActiveMQArtemisClientException
    {
        public ProducerClosedException() : base("The Producer was closed.")
        {
        }

        public ProducerClosedException(Exception innerException) : base("The Producer was closed.", innerException)
        {
        }

        public ProducerClosedException(string message, string errorCode, Exception innerException) : base(message, errorCode, innerException)
        {
        }
    }
}