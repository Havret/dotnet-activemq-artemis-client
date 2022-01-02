using System.Threading.Channels;

namespace ActiveMQ.Artemis.Client.Testing;

internal class Subscription : ISubscription
{
    public EventHandler? Disposed = null;

    private readonly ChannelReader<Message> _reader;
    private readonly ChannelWriter<Message> _writer;

    public Subscription()
    {
        var channel = Channel.CreateUnbounded<Message>();
        _reader = channel.Reader;
        _writer = channel.Writer;
    }

    public ValueTask<Message> ReceiveAsync(CancellationToken cancellationToken)
    {
        return _reader.ReadAsync(cancellationToken);
    }

    public void OnMessage(Message message)
    {
        _writer.TryWrite(message);
    }

    public void Dispose()
    {
        _writer.Complete();
        Disposed?.Invoke(this, EventArgs.Empty);
    }
}