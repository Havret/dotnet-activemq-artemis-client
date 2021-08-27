using System;

namespace ActiveMQ.Artemis.Client.MessageIdPolicy
{
    internal class StringGuidMessageIdPolicy : IMessageIdPolicy
    {
        public object GetNextMessageId()
        {
            return Guid.NewGuid().ToString("N");
        }
    }
}