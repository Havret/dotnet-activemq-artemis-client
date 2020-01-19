using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Amqp;

namespace ActiveMQ.Net
{
    public class Consumer : IConsumer
    {
        private readonly ReceiverLink _receiverLink;
        private readonly ChannelReader<Message> _reader;
        private readonly ChannelWriter<Message> _writer;

        public Consumer(ReceiverLink receiverLink)
        {
            _receiverLink = receiverLink;
            var channel = Channel.CreateBounded<Message>(200);
            _reader = channel.Reader;
            _writer = channel.Writer;
            _receiverLink.Start(200, (receiver, m) =>
            {
                _writer.TryWrite(new Message(m));
            });
        }

        public ValueTask<Message> ReceiveAsync(CancellationToken cancellationToken = default)
        {
            return _reader.ReadAsync(cancellationToken);
        }

        public void Accept(Message message)
        {
            _receiverLink.Accept(message.InnerMessage);
        }

        public void Reject(Message message)
        {
            _receiverLink.Reject(message.InnerMessage);
        }

        public async ValueTask DisposeAsync()
        {
            _writer.TryComplete();
            if (!_receiverLink.IsClosed)
            {
                await _receiverLink.CloseAsync().ConfigureAwait(false);
            }
        }
    }
}