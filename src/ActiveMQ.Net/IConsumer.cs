using System;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Net.Transactions;

namespace ActiveMQ.Net
{
    public interface IConsumer : IAsyncDisposable
    {
        ValueTask<Message> ReceiveAsync(CancellationToken cancellationToken = default);
        ValueTask AcceptAsync(Message message, Transaction transaction, CancellationToken cancellationToken = default);
        void Reject(Message message);
    }
}