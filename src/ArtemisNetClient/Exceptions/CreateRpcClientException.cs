namespace ActiveMQ.Artemis.Client.Exceptions
{
    internal class CreateRpcClientException : ActiveMQArtemisClientException
    {
        public CreateRpcClientException(string message, string errorCode) : base(message, errorCode)
        {
        }
    }
}