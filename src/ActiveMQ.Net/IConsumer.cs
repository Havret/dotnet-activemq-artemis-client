using System;
using System.Threading;
using System.Threading.Tasks;

namespace ActiveMQ.Net
{
    public interface IConsumer : IAsyncDisposable
    {
        ValueTask<Message> ConsumeAsync(CancellationToken cancellationToken = default);
        void Accept(Message message);
        void Reject(Message message);
    }
}