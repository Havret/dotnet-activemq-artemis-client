using System.Collections.Concurrent;
using Amqp.Listener;

namespace ActiveMQ.Net.Tests.Utils
{
    public class MessageProcessor : IMessageProcessor
    {
        private readonly ConcurrentQueue<Message> _messages = new ConcurrentQueue<Message>();

        void IMessageProcessor.Process(MessageContext messageContext)
        {
            _messages.Enqueue(new Message(messageContext.Message));
            messageContext.Complete();
        }

        int IMessageProcessor.Credit { get; } = 30;

        public Message Dequeue()
        {
            if (_messages.TryDequeue(out  var message))
            {
                return message;
            }

            return null;
        }
    }
}