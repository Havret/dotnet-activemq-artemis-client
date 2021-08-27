namespace ActiveMQ.Artemis.Client.MessageIdPolicy
{
    public interface IMessageIdPolicy
    {
        object GetNextMessageId();
    }
}