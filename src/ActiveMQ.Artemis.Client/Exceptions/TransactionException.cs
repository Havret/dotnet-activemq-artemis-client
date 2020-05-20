namespace ActiveMQ.Artemis.Client.Exceptions
{
    public class TransactionException : ActiveMQArtemisClientException
    {
        public TransactionException(string message, string errorCode) : base(message, errorCode)
        {
        }
    }
}