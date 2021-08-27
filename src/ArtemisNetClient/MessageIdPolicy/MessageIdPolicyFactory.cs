namespace ActiveMQ.Artemis.Client.MessageIdPolicy
{
    public static class MessageIdPolicyFactory
    {
        public static IMessageIdPolicy DisableMessageIdPolicy()
        {
            return new DisableMessageIdPolicy();
        }
        
        public static IMessageIdPolicy GuidMessageIdPolicy()
        {
            return new GuidMessageIdPolicy();
        }
        
        public static IMessageIdPolicy StringGuidMessageIdPolicy()
        {
            return new StringGuidMessageIdPolicy();
        }
    }
}