namespace ActiveMQ.Artemis.Client.Exceptions
{
    public class ConnectionClosedException : ActiveMQArtemisClientException
    {
        public ConnectionClosedException(string message, string errorCode) : base(message, errorCode)
        {
        }
    }
}