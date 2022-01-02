namespace ActiveMQ.Artemis.Client.Testing;

public interface ISubscription : IDisposable
{
    ValueTask<Message> ReceiveAsync(CancellationToken cancellationToken = default);
}