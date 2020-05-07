using System;
using System.Threading;
using System.Threading.Tasks;

namespace ActiveMQ.Artemis.Client.Transactions
{
    public class Transaction : IAsyncDisposable
    {
        private bool _complete;

        private TransactionCoordinator _transactionCoordinator;
        internal byte[] TxnId { get; private set; }

        internal void Initialize(byte[] txnId, TransactionCoordinator transactionCoordinator)
        {
            TxnId = txnId;
            _transactionCoordinator = transactionCoordinator;
        }

        internal bool IsEnlisted()
        {
            if (_complete)
                throw new InvalidOperationException("Transaction completed.");
            
            return TxnId != null;
        }

        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            if (_complete)
                throw new InvalidOperationException("Cannot commit completed transaction.");

            await _transactionCoordinator.DischargeAsync(TxnId, false, cancellationToken).ConfigureAwait(false);
            _complete = true;
        }

        public async Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            if (_complete)
                throw new InvalidOperationException("Cannot rollback completed transaction.");

            await _transactionCoordinator.DischargeAsync(TxnId, true, cancellationToken).ConfigureAwait(false);
            _complete = true;
        }

        public async ValueTask DisposeAsync()
        {
            if (!_complete)
            {
                await RollbackAsync().ConfigureAwait(false);
            }
        }
    }
}