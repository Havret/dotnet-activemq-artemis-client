namespace ActiveMQ.Artemis.Client.Exceptions
{
    public class CreateConnectionException : ActiveMQArtemisClientException
    {
        public CreateConnectionException(string message, string errorCode) : base(message, errorCode)
        {
        }

        public CreateConnectionException(string message) : base(message)
        {
        }
    }
}