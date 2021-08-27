namespace ActiveMQ.Artemis.Client.Exceptions
{
    public class CreateProducerException : ActiveMQArtemisClientException
    {
        public CreateProducerException(string message, string errorCode) : base(message, errorCode)
        {
        }
    }
}