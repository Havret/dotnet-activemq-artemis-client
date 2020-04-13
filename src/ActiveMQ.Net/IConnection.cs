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
        Task<IConsumer> CreateConsumerAsync(ConsumerConfiguration configuration, CancellationToken cancellationToken);
        Task<IProducer> CreateProducerAsync(ProducerConfiguration configuration, CancellationToken cancellationToken);
        event EventHandler<ConnectionClosedEventArgs> ConnectionClosed;
        event EventHandler<ConnectionRecoveredEventArgs> ConnectionRecovered;
        event EventHandler<ConnectionRecoveryErrorEventArgs> ConnectionRecoveryError;
    }
}