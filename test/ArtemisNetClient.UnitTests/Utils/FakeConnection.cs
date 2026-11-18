using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Transactions;

namespace ActiveMQ.Artemis.Client.UnitTests.Utils
{
    /// <summary>
    /// Hands out the given resources in order, one per creation, letting tests drive the recoverables directly.
    /// </summary>
    internal class FakeConnection(params IAsyncDisposable[] resources) : IConnection
    {
        private readonly Queue<IAsyncDisposable> _resources = new(resources);

        public Endpoint Endpoint => null;
        public bool IsOpened => true;

        public Task<ITopologyManager> CreateTopologyManagerAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IConsumer> CreateConsumerAsync(ConsumerConfiguration configuration, CancellationToken cancellationToken = default) => Next<IConsumer>();
        public Task<IProducer> CreateProducerAsync(ProducerConfiguration configuration, CancellationToken cancellationToken = default) => Next<IProducer>();
        public Task<IAnonymousProducer> CreateAnonymousProducerAsync(AnonymousProducerConfiguration configuration, CancellationToken cancellationToken = default) => Next<IAnonymousProducer>();
        public Task<IRequestReplyClient> CreateRequestReplyClientAsync(RequestReplyClientConfiguration configuration, CancellationToken cancellationToken = default) => Next<IRequestReplyClient>();

        private Task<T> Next<T>() => Task.FromResult((T) _resources.Dequeue());

        public event EventHandler<ConnectionClosedEventArgs> ConnectionClosed { add { } remove { } }
        public event EventHandler<ConnectionRecoveredEventArgs> ConnectionRecovered { add { } remove { } }
        public event EventHandler<ConnectionRecoveryErrorEventArgs> ConnectionRecoveryError { add { } remove { } }

        public ValueTask DisposeAsync() => default;
    }

    /// <summary>
    /// Keeps every request pending until the test completes it.
    /// </summary>
    internal class FakeRequestReplyClient : IRequestReplyClient
    {
        public TaskCompletionSource<bool> RequestSent { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<Message> PendingRequest { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool IsDisposed { get; private set; }

        public Task<Message> SendAsync(string address, RoutingType? routingType, Message message, CancellationToken cancellationToken)
        {
            RequestSent.TrySetResult(true);
            return PendingRequest.Task;
        }

        public ValueTask DisposeAsync()
        {
            IsDisposed = true;
            return default;
        }
    }

    /// <summary>
    /// Keeps every receive pending until the test completes it.
    /// </summary>
    internal class FakeConsumer : IConsumer
    {
        public TaskCompletionSource<bool> ReceiveCalled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<Message> PendingReceive { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public ValueTask<Message> ReceiveAsync(CancellationToken cancellationToken = default)
        {
            ReceiveCalled.TrySetResult(true);
            return new ValueTask<Message>(PendingReceive.Task);
        }

        public ValueTask AcceptAsync(Message message, Transaction transaction, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public void Modify(Message message, bool deliveryFailed, bool undeliverableHere) => throw new NotSupportedException();
        public void Reject(Message message) => throw new NotSupportedException();
        public ValueTask DisposeAsync() => default;
    }

    /// <summary>
    /// Keeps every send pending until the test completes it.
    /// </summary>
    internal class FakeAnonymousProducer : IAnonymousProducer
    {
        public TaskCompletionSource<bool> MessageSent { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<bool> PendingSend { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool IsDisposed { get; private set; }

        public Task SendAsync(string address, RoutingType? routingType, Message message, Transaction transaction, CancellationToken cancellationToken = default)
        {
            MessageSent.TrySetResult(true);
            return PendingSend.Task;
        }

        public void Send(string address, RoutingType? routingType, Message message, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public ValueTask DisposeAsync()
        {
            IsDisposed = true;
            return default;
        }
    }
}
