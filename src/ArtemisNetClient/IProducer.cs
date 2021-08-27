using System;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Transactions;

namespace ActiveMQ.Artemis.Client
{
    public interface IProducer : IAsyncDisposable
    {
        Task SendAsync(Message message, Transaction transaction, CancellationToken cancellationToken = default);
        void Send(Message message, CancellationToken cancellationToken = default);
    }
}