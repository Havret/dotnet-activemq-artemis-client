using System;

namespace ActiveMQ.Artemis.Client.Exceptions
{
    public class MessageSendException : ActiveMQArtemisClientException
    {
        public MessageSendException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public MessageSendException(string message, string errorCode) : base(message, errorCode)
        {
        }

        public MessageSendException(string message, string errorCode, Exception innerException) : base(message, errorCode, innerException)
        {
        }
    }
}