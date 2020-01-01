using System;
using System.Threading.Tasks;

namespace ActiveMQ.Net
{
    public interface IProducer : IAsyncDisposable
    {
        Task ProduceAsync(Message message);
        void Produce(Message message);
    }
}