using System;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Net.Transactions;

namespace ActiveMQ.Net
{
    public interface IAnonymousProducer : IAsyncDisposable
    {
        Task SendAsync(string address, AddressRoutingType routingType, Message message, Transaction transaction, CancellationToken cancellationToken = default);
        void Send(string address, AddressRoutingType routingType, Message message);
    }
}