using System.Threading;
using System.Threading.Tasks;
using Amqp;
using Amqp.Framing;

namespace ActiveMQ.Net
{
    internal class Producer : IProducer
    {
        private readonly SenderLink _senderLink;

        public Producer(SenderLink senderLink)
        {
            _senderLink = senderLink;
        }

        public Task ProduceAsync(Message message, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            cancellationToken.Register(() => tcs.SetCanceled());
            _senderLink.Send(message.InnerMessage, null, OnOutcome, tcs);
            return tcs.Task;
        }

        private static void OnOutcome(ILink sender, Amqp.Message message, Outcome outcome, object state)
        {
            var tcs = (TaskCompletionSource<bool>) state;
            if (outcome.Descriptor.Code == MessageOutcomes.Accepted.Descriptor.Code)
            {
                tcs.TrySetResult(true);
            }
            else if (outcome.Descriptor.Code == MessageOutcomes.Rejected.Descriptor.Code)
            {
                tcs.TrySetException(MessageSendException.FromError(((Rejected)outcome).Error));
            }
            else if (outcome.Descriptor.Code == MessageOutcomes.Released.Descriptor.Code)
            {
                tcs.TrySetException(new MessageSendException(ErrorCode.MessageReleased, "Message was released by remote peer."));
            }
            else
            {
                tcs.TrySetException(new MessageSendException(ErrorCode.InternalError, outcome.ToString()));
            }
        }

        public void Produce(Message message)
        {
            _senderLink.Send(message.InnerMessage, null, null, null);
        }

        public async ValueTask DisposeAsync()
        {
            await _senderLink.CloseAsync().ConfigureAwait(false);
        }
    }
}