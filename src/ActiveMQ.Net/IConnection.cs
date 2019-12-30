using System;
using System.Threading.Tasks;

namespace ActiveMQ.Net
{
    public interface IConnection : IAsyncDisposable
    {
        Task<IConsumer> CreateConsumerAsync(string address);
        Task<IConsumer> CreateConsumerAsync(string address, RoutingType routingType);
        Task<IConsumer> CreateConsumerAsync(string address, RoutingType routingType, string queue);        
        IProducer CreateProducer(string address);
        IProducer CreateProducer(string address, RoutingType routingType);
    }
}