using System.Threading.Channels;
using System.Threading.Tasks;
using Amqp;

namespace ActiveMQ.Net
{
    public class Consumer : IConsumer
    {
        private readonly ReceiverLink _receiverLink;
        private readonly ChannelReader<Message> _reader;
        private ChannelWriter<Message> _writer;

        public Consumer(ReceiverLink receiverLink)
        {
            _receiverLink = receiverLink;
            var channel = Channel.CreateBounded<Message>(200);
            _reader = channel.Reader;
            _writer = channel.Writer;
            _receiverLink.Start(200, (receiver, m) =>
            {
                var message = new Message(m);
                _writer.TryWrite(message);
            });
        }

        public ValueTask<Message> ConsumeAsync()
        {
            return _reader.ReadAsync();
        }

        public async ValueTask DisposeAsync()
        {
            await _receiverLink.CloseAsync().ConfigureAwait(false);
        }
    }
}