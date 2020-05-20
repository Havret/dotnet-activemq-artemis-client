namespace ActiveMQ.Artemis.Client.Exceptions
{
    public class CreateTransactionCoordinatorException : ActiveMQArtemisClientException
    {
        public CreateTransactionCoordinatorException(string message, string errorCode) : base(message, errorCode)
        {
        }
    }
}