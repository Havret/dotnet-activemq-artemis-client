using System;

namespace ActiveMQ.Artemis.Client.MessageIdPolicy
{
    internal class GuidMessageIdPolicy : IMessageIdPolicy
    {
        public object GetNextMessageId()
        {
            return Guid.NewGuid();
        }
    }
}