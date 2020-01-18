using System;
using System.Threading.Tasks;
using Amqp;

namespace ActiveMQ.Net
{
    public interface IConnection : IAsyncDisposable
    {
        bool IsClosed { get; }
        Task<IConsumer> CreateConsumerAsync(string address, RoutingType routingType);
        Task<IProducer> CreateProducerAsync(string address, RoutingType routingType);
        event EventHandler<ConnectionClosedEventArgs> ConnectionClosed;
    }
}