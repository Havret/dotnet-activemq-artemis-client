using System;

namespace ActiveMQ.Artemis.Client.Exceptions
{
    public abstract class ActiveMQArtemisClientException : Exception
    {
        protected ActiveMQArtemisClientException(string message) : base(message)
        {
        }

        protected ActiveMQArtemisClientException(string message, string errorCode) : this(message)
        {
            ErrorCode = errorCode;
        }
        
        protected ActiveMQArtemisClientException(string message, string errorCode, Exception innerException) : this(message, innerException)
        {
            ErrorCode = errorCode;
        }

        protected ActiveMQArtemisClientException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public string ErrorCode { get; }
    }
}