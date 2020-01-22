using System;
using System.Threading;
using System.Threading.Tasks;

namespace ActiveMQ.Net
{
    public interface IConnection : IAsyncDisposable
    {
        bool IsOpened { get; }
        Task<IConsumer> CreateConsumerAsync(string address, RoutingType routingType, CancellationToken cancellationToken);
        Task<IProducer> CreateProducerAsync(string address, RoutingType routingType);
        event EventHandler<ConnectionClosedEventArgs> ConnectionClosed;
    }
}