using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Exceptions;
using ActiveMQ.Artemis.Client.InternalUtilities;
using Amqp;
using Amqp.Framing;
using Amqp.Transactions;

namespace ActiveMQ.Artemis.Client.Transactions
{
    internal class TransactionCoordinator
    {
        private static readonly OutcomeCallback _onDeclareOutcome = OnDeclareOutcome;
        private static readonly OutcomeCallback _onDischargeOutcome = OnDischargeOutcome;

        private readonly SenderLink _senderLink;

        public TransactionCoordinator(SenderLink senderLink)
        {
            _senderLink = senderLink;
        }
        
        public async Task<byte[]> DeclareAsync(CancellationToken cancellationToken)
        {
            var message = new Amqp.Message(new Declare());
            var (tcs, ctr) = TaskUtil.CreateTaskCompletionSource<byte[]>(ref cancellationToken);
            using var _ = ctr;
            _senderLink.Send(message, null, _onDeclareOutcome, tcs);
            return await tcs.Task.ConfigureAwait(false);
        }

        public async Task DischargeAsync(byte[] txnId, bool fail, CancellationToken cancellationToken)
        {
            var message = new Amqp.Message(new Discharge { TxnId = txnId, Fail = fail });
            var (tcs, ctr) = TaskUtil.CreateTaskCompletionSource<bool>(ref cancellationToken);
            using var _ = ctr;
            _senderLink.Send(message, null, _onDischargeOutcome, tcs);
            await tcs.Task.ConfigureAwait(false);
        }
        
        private static void OnDeclareOutcome(ILink link, Amqp.Message message, Outcome outcome, object state)
        {
            var tcs = (TaskCompletionSource<byte[]>) state;
            if (outcome.Descriptor.Code == MessageOutcomes.Declared.Descriptor.Code)
            {
                tcs.SetResult(((Declared) outcome).TxnId);
            }
            else if (outcome.Descriptor.Code == MessageOutcomes.Rejected.Descriptor.Code)
            {
                var rejected = (Rejected) outcome;
                tcs.SetException(new TransactionException(rejected.Error?.Description, rejected.Error?.Condition));
            }
            else
            {
                tcs.SetCanceled();
            }
        }

        private static void OnDischargeOutcome(ILink link, Amqp.Message message, Outcome outcome, object state)
        {
            var tcs = (TaskCompletionSource<bool>) state;
            if (outcome.Descriptor.Code == MessageOutcomes.Accepted.Descriptor.Code)
            {
                tcs.SetResult(true);
            }
            else if (outcome.Descriptor.Code == MessageOutcomes.Rejected.Descriptor.Code)
            {
                var rejected = (Rejected) outcome;
                tcs.SetException(new TransactionException(rejected.Error?.Description, rejected.Error?.Condition));
            }
            else
            {
                tcs.SetCanceled();
            }
        }
    }
}