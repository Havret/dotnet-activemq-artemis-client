using System;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Transactions;

namespace ActiveMQ.Artemis.Client
{
    public interface IAnonymousProducer : IAsyncDisposable
    {
        Task SendAsync(string address, RoutingType? routingType, Message message, Transaction transaction, CancellationToken cancellationToken = default);
        void Send(string address, RoutingType? routingType, Message message, CancellationToken cancellationToken = default);
    }
}