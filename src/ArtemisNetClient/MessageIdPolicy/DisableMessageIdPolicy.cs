namespace ActiveMQ.Artemis.Client.MessageIdPolicy
{
    internal class DisableMessageIdPolicy : IMessageIdPolicy
    {
        public object GetNextMessageId()
        {
            return null;
        }
    }
}