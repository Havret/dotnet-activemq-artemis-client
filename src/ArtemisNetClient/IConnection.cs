using System;
using System.Threading;
using System.Threading.Tasks;

namespace ActiveMQ.Artemis.Client
{
    public interface IConnection : IAsyncDisposable
    {
        /// <summary>
        /// Retrieve the endpoint this connection is connected to.
        /// </summary>
        Endpoint Endpoint { get; }
        bool IsOpened { get; }
        Task<ITopologyManager> CreateTopologyManagerAsync(CancellationToken cancellationToken = default); 
        Task<IConsumer> CreateConsumerAsync(ConsumerConfiguration configuration, CancellationToken cancellationToken = default);
        Task<IProducer> CreateProducerAsync(ProducerConfiguration configuration, CancellationToken cancellationToken = default);
        Task<IAnonymousProducer> CreateAnonymousProducerAsync(AnonymousProducerConfiguration configuration, CancellationToken cancellationToken = default);
        Task<IRpcClient> CreateRpcClientAsync(CancellationToken cancellationToken = default);
        event EventHandler<ConnectionClosedEventArgs> ConnectionClosed;
        event EventHandler<ConnectionRecoveredEventArgs> ConnectionRecovered;
        event EventHandler<ConnectionRecoveryErrorEventArgs> ConnectionRecoveryError;
    }
}