using System;
using System.Threading.Tasks;

namespace ActiveMQ.Net
{
    public interface IConnection : IAsyncDisposable
    {
        Task<IConsumer> CreateConsumerAsync(string address, RoutingType routingType);
        Task<IProducer> CreateProducer(string address, RoutingType routingType);
    }
}