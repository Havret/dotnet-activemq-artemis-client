using System.Threading.Channels;
using Amqp.Listener;

namespace ActiveMQ.Artemis.Client.Testing;

internal class MessageSource : IMessageSource
{
    private readonly ChannelReader<Message> _reader;
    private readonly ChannelWriter<Message> _writer;

    public MessageSource()
    {
        var channel = Channel.CreateUnbounded<Message>();
        _reader = channel.Reader;
        _writer = channel.Writer;
    }

    public void Enqueue(Message message)
    {
        _writer.TryWrite(message);
    }

    async Task<ReceiveContext> IMessageSource.GetMessageAsync(ListenerLink link)
    {
        var msg = await _reader.ReadAsync().ConfigureAwait(false);
        return new ReceiveContext(link, msg.InnerMessage);
    }

    void IMessageSource.DisposeMessage(ReceiveContext receiveContext, DispositionContext dispositionContext)
    {
    }
}