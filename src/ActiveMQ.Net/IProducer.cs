using System;
using System.Threading;
using System.Threading.Tasks;

namespace ActiveMQ.Net
{
    public interface IProducer : IAsyncDisposable
    {
        Task ProduceAsync(Message message, CancellationToken cancellationToken = default);
        void Produce(Message message);
    }
}