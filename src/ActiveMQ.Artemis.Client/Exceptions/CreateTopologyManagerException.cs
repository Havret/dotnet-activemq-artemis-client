using System;

namespace ActiveMQ.Artemis.Client.Exceptions
{
    public class CreateTopologyManagerException : ActiveMQArtemisClientException
    {
        public CreateTopologyManagerException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}