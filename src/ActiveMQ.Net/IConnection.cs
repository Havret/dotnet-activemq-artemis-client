using System;
using System.Threading;
using System.Threading.Tasks;

namespace ActiveMQ.Net
{
    public interface IConnection : IAsyncDisposable
    {
        /// <summary>
        /// Retrieve the endpoint this connection is connected to.
        /// </summary>
        Endpoint Endpoint { get; }
        bool IsOpened { get; }
        Task<IConsumer> CreateConsumerAsync(string address, RoutingType routingType, CancellationToken cancellationToken);
        Task<IProducer> CreateProducerAsync(string address, RoutingType routingType, CancellationToken cancellationToken);
        event EventHandler<ConnectionClosedEventArgs> ConnectionClosed;
        event EventHandler<ConnectionRecoveredEventArgs> ConnectionRecovered;
    }
}