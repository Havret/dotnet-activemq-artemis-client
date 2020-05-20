using System;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Exceptions;
using ActiveMQ.Artemis.Client.Transactions;
using Amqp;
using Amqp.Framing;
using Amqp.Transactions;

namespace ActiveMQ.Artemis.Client.Builders
{
    internal class TransactionCoordinatorBuilder
    {
        private readonly Session _session;
        private readonly TaskCompletionSource<bool> _tcs;

        public TransactionCoordinatorBuilder(Session session)
        {
            _session = session;
            _tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public async Task<TransactionCoordinator> CreateAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            cancellationToken.Register(() => _tcs.TrySetCanceled());

            var attach = new Attach
            {
                Target = new Coordinator
                {
                    Capabilities = new[] { TxnCapabilities.LocalTransactions }
                },
                Source = new Source(),
                SndSettleMode = SenderSettleMode.Unsettled,
                RcvSettleMode = ReceiverSettleMode.First
            };
            var senderLink = new SenderLink(_session, GetName(), attach, OnAttached);
            senderLink.AddClosedCallback(OnClosed);
            await _tcs.Task.ConfigureAwait(false);
            var transactionCoordinator = new TransactionCoordinator(senderLink);
            senderLink.Closed -= OnClosed;
            return transactionCoordinator;
        }
        
        private static string GetName()
        {
            return "coordinator-link-" + Guid.NewGuid().ToString("N").Substring(0, 5);
        }
        
        private void OnAttached(ILink link, Attach attach)
        {
            if (attach.Source != null)
            {
                _tcs.TrySetResult(true);
            }
        }
        
        private void OnClosed(IAmqpObject sender, Error error)
        {
            if (error != null)
            {
                _tcs.TrySetException(new CreateTransactionCoordinatorException(error.Description, error.Condition));
            }
        }
    }
}