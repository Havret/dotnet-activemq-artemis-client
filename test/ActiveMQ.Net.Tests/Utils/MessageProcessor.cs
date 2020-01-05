using System;
using System.Collections.Concurrent;
using Amqp.Listener;

namespace ActiveMQ.Net.Tests.Utils
{
    public class MessageProcessor : IMessageProcessor
    {
        private readonly BlockingCollection<Message> _messages = new BlockingCollection<Message>();

        void IMessageProcessor.Process(MessageContext messageContext)
        {
            _messages.TryAdd(new Message(messageContext.Message));
            messageContext.Complete();
        }

        int IMessageProcessor.Credit { get; } = 30;

        public Message Dequeue(TimeSpan timeout)
        {
            if (_messages.TryTake(out  var message, timeout))
            {
                return message;
            }

            return null;
        }
    }
}