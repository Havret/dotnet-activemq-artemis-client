using System;
using System.Collections.Concurrent;
using Amqp.Listener;

namespace ActiveMQ.Artemis.Client.UnitTests.Utils
{
    public class MessageProcessor : IMessageProcessor
    {
        private readonly BlockingCollection<Message> _messages = new BlockingCollection<Message>();
        private Func<MessageContext, bool> _messageHandler;
        
        public void SetHandler(Func<MessageContext, bool> messageHandler)
        {
            _messageHandler = messageHandler;
        }

        void IMessageProcessor.Process(MessageContext messageContext)
        {
            if (_messageHandler != null)
            {
                if (_messageHandler(messageContext))
                {
                    return;
                }
            }

            _messages.TryAdd(new Message(messageContext.Message));
            messageContext.Complete();
        }

        int IMessageProcessor.Credit { get; } = 30;

        public Message Dequeue(TimeSpan timeout)
        {
            if (_messages.TryTake(out var message, timeout))
            {
                return message;
            }

            return null;
        }
    }
}