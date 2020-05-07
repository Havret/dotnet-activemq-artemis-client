using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Exceptions;
using Amqp;
using Amqp.Framing;
using Amqp.Transactions;
using TaskExtensions = ActiveMQ.Artemis.Client.InternalUtilities.TaskExtensions;

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
        
        public Task<byte[]> DeclareAsync(CancellationToken cancellationToken)
        {
            var message = new Amqp.Message(new Declare());
            var tcs = TaskExtensions.CreateTaskCompletionSource<byte[]>(cancellationToken);
            _senderLink.Send(message, null, _onDeclareOutcome, tcs);
            return tcs.Task;
        }

        public Task DischargeAsync(byte[] txnId, bool fail, CancellationToken cancellationToken)
        {
            var message = new Amqp.Message(new Discharge { TxnId = txnId, Fail = fail });
            var tcs = TaskExtensions.CreateTaskCompletionSource<bool>(cancellationToken);
            _senderLink.Send(message, null, _onDischargeOutcome, tcs);
            return tcs.Task;
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
                tcs.SetException(TransactionException.FromError(((Rejected) outcome).Error));
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
                tcs.SetException(TransactionException.FromError(((Rejected) outcome).Error));
            }
            else
            {
                tcs.SetCanceled();
            }
        }
    }
}