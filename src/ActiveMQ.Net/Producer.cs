using System.Threading.Tasks;
using Amqp;

namespace ActiveMQ.Net
{
    internal class Producer : IProducer
    {
        private readonly SenderLink _senderLink;

        public Producer(SenderLink senderLink)
        {
            _senderLink = senderLink;
        }

        public Task ProduceAsync(Message message)
        {
            return _senderLink.SendAsync(message.InnerMessage);
        }

        public async ValueTask DisposeAsync()
        {
            await _senderLink.CloseAsync().ConfigureAwait(false);
        }
    }
}