using System;
using System.Threading;
using System.Threading.Tasks;

namespace ActiveMQ.Net
{
    public interface IAnonymousProducer : IAsyncDisposable
    {
        Task SendAsync(string address, AddressRoutingType routingType, Message message, CancellationToken cancellationToken = default);
        void Send(string address, AddressRoutingType routingType, Message message);
    }
}