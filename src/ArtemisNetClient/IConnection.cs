using System;
using System.Threading;
using System.Threading.Tasks;

namespace ActiveMQ.Artemis.Client
{
    /// <summary>
    /// Main interface to an ActiveMQ Artemis connection.
    /// </summary>
    /// <remarks>
    /// Instances of <see cref="IConnection"/> are used to create producers, anonymous producers, consumers,
    /// topology managers, and request-reply clients.
    ///
    /// The <see cref="ConnectionFactory"/> class is used to construct <see cref="IConnection"/> instances.
    /// </remarks>
    public interface IConnection : IAsyncDisposable
    {
        /// <summary>
        /// Retrieve the endpoint this connection is connected to.
        /// </summary>
        Endpoint Endpoint { get; }
        
        /// <summary>
        /// Returns true if the connection is still in a state where it can be used.
        /// </summary>
        bool IsOpened { get; }
        Task<ITopologyManager> CreateTopologyManagerAsync(CancellationToken cancellationToken = default); 
        Task<IConsumer> CreateConsumerAsync(ConsumerConfiguration configuration, CancellationToken cancellationToken = default, bool isBrowser = false);
        Task<IProducer> CreateProducerAsync(ProducerConfiguration configuration, CancellationToken cancellationToken = default);
        Task<IAnonymousProducer> CreateAnonymousProducerAsync(AnonymousProducerConfiguration configuration, CancellationToken cancellationToken = default);
        Task<IRequestReplyClient> CreateRequestReplyClientAsync(RequestReplyClientConfiguration configuration, CancellationToken cancellationToken = default);
        Task<IBrowser> CreateBrowserAsync(ConsumerConfiguration configuration, CancellationToken cancellationToken = default);

        /// <summary>
        /// Raised when the connection is closed.
        /// </summary>
        /// <remarks>
        /// If the connection is already closed at the time a subscription is added to this event,
        /// the event handler will be raised immediately.
        /// 
        /// When the connection was created with enabled automatic recovery, this event will be raised every time
        /// the connection to the remote peer has been lost.
        /// </remarks>
        event EventHandler<ConnectionClosedEventArgs> ConnectionClosed;
        event EventHandler<ConnectionRecoveredEventArgs> ConnectionRecovered;
        event EventHandler<ConnectionRecoveryErrorEventArgs> ConnectionRecoveryError;
    }
}