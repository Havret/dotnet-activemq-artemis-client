using System.Threading;
using System.Threading.Tasks;
using Nito.AsyncEx;

namespace ActiveMQ.Net.Transactions
{
    internal class TransactionsManager
    {
        private readonly AsyncLock _mutex = new AsyncLock();
        private readonly Connection _connection;
        private TransactionCoordinator _transactionCoordinator;

        public TransactionsManager(Connection connection)
        {
            _connection = connection;
        }

        public async ValueTask<byte[]> GetTxnIdAsync(Transaction transaction, CancellationToken cancellationToken)
        {
            if (transaction == null)
            {
                return null;
            }

            if (transaction.IsEnlisted())
            {
                return transaction.TxnId;
            }

            using (await _mutex.LockAsync().ConfigureAwait(false))
            {
                await EnlistAsync(transaction, cancellationToken).ConfigureAwait(false);
            }

            return transaction.TxnId;
        }

        private async Task EnlistAsync(Transaction transaction, CancellationToken cancellationToken)
        {
            var transactionCoordinator = await GetOrCreate(cancellationToken).ConfigureAwait(false);
            var txnId = await transactionCoordinator.DeclareAsync(cancellationToken).ConfigureAwait(false);
            transaction.Initialize(txnId, transactionCoordinator);
        }

        private async ValueTask<TransactionCoordinator> GetOrCreate(CancellationToken cancellationToken)
        {
            return _transactionCoordinator ??= await _connection.CreateTransactionCoordinator(cancellationToken).ConfigureAwait(false);
        }
    }
}