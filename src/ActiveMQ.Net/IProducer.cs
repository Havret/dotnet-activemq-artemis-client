using System;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Net.Transactions;

namespace ActiveMQ.Net
{
    public interface IProducer : IAsyncDisposable
    {
        Task SendAsync(Message message, Transaction transaction, CancellationToken cancellationToken = default);
        void Send(Message message);
    }
}