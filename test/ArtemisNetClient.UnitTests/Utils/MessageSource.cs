using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Amqp.Listener;

namespace ActiveMQ.Artemis.Client.UnitTests.Utils
{
    public class MessageSource : IMessageSource
    {
        private readonly ConcurrentQueue<Message> _messages = new ConcurrentQueue<Message>();
        private readonly BlockingCollection<DispositionContext> _dispositions = new BlockingCollection<DispositionContext>();

        public void Enqueue(Message message)
        {
            _messages.Enqueue(message);
        }

        public DispositionContext GetNextDisposition(TimeSpan timeout)
        {
            if (_dispositions.TryTake(out var dispositionContext, timeout))
            {
                return dispositionContext;
            }
            
            return default;
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
            _dispositions.TryAdd(dispositionContext);
        }
    }
}