using System;

namespace ActiveMQ.Artemis.Client.Exceptions
{
    public class CreateEndpointException : ActiveMQArtemisClientException
    {
        public CreateEndpointException(string message, string errorCode) : base(message, errorCode)
        {
        }

        public CreateEndpointException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public CreateEndpointException(string message, string errorCode, Exception innerException) : base(message, errorCode, innerException)
        {
        }
    }
}