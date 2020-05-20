namespace ActiveMQ.Artemis.Client.Exceptions
{
    public class CreateConsumerException : ActiveMQArtemisClientException
    {
        public CreateConsumerException(string message, string errorCode) : base(message, errorCode)
        {
        }
    }
}