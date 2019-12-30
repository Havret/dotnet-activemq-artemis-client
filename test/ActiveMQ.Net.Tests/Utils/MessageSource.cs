using System.Collections.Concurrent;
using System.Threading.Tasks;
using Amqp.Listener;

namespace ActiveMQ.Net.Tests.Utils
{
    public class MessageSource : IMessageSource
    {
        private readonly ConcurrentQueue<Message> _messages = new ConcurrentQueue<Message>();

        public void Enqueue(Message message)
        {
            _messages.Enqueue(message);
        }

        Task<ReceiveContext> IMessageSource.GetMessageAsync(ListenerLink link)
        {
            if (_messages.TryDequeue(out var message))
            {
                return Task.FromResult(new ReceiveContext(link, message.InnerMessage));
            }

            return Task.FromResult<ReceiveContext>(null);
        }

        void IMessageSource.DisposeMessage(ReceiveContext receiveContext, DispositionContext dispositionContext)
        {
            
        }
    }
}