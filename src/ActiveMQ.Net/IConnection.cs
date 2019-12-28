using System;

namespace ActiveMQ.Net
{
    public interface IConnection : IAsyncDisposable
    {
        IConsumer CreateConsumer(string address);
        IConsumer CreateConsumer(string address, RoutingType routingType);
        IConsumer CreateConsumer(string address, RoutingType routingType, string queue);
        IConsumer CreateConsumer(string address, RoutingType routingType, ConsumerConfig config);
        IProducer CreateProducer(string address);
        IProducer CreateProducer(string address, RoutingType routingType);
    }
}