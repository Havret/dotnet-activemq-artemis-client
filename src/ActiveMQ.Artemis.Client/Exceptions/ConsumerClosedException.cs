using System;

namespace ActiveMQ.Artemis.Client.Exceptions
{
    public class ConsumerClosedException : ActiveMQArtemisClientException
    {
        public ConsumerClosedException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public ConsumerClosedException(Exception innerException) : base("The Consumer was closed.", innerException)
        {
        }

        public ConsumerClosedException() : base("The Consumer was closed.")
        {
        }

        public ConsumerClosedException(string message, string errorCode) : base(message ?? "The Consumer was closed.", errorCode)
        {
        }
    }
}