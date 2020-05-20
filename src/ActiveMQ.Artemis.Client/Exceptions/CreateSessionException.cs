namespace ActiveMQ.Artemis.Client.Exceptions
{
    public class CreateSessionException : ActiveMQArtemisClientException
    {
        public CreateSessionException(string message, string errorCode) : base(message, errorCode)
        {
        }
    }
}