using System;
using System.Threading;
using System.Threading.Tasks;

namespace ActiveMQ.Net
{
    public interface IProducer : IAsyncDisposable
    {
        Task SendAsync(Message message, CancellationToken cancellationToken = default);
        void Send(Message message);
    }
}