using System;
using System.Threading.Tasks;

namespace ActiveMQ.Net
{
    public interface IConnection : IAsyncDisposable
    {
        IConsumer CreateConsumer(string address);
        IConsumer CreateConsumer(string address, RoutingType routingType);
        IConsumer CreateConsumer(string address, RoutingType routingType, ConsumerConfig config);
        IProducer CreateProducer(string address, RoutingType routingType);
        Task CloseAsync();
    }
}