using System;
using System.Threading;
using System.Threading.Tasks;
using Amqp;
using Amqp.Framing;

namespace ActiveMQ.Net
{
    internal class Producer : IProducer
    {
        private static readonly OutcomeCallback _onOutcome = OnOutcome;
        private readonly SenderLink _senderLink;

        public Producer(SenderLink senderLink)
        {
            _senderLink = senderLink;
        }

        private bool IsDetaching => _senderLink.LinkState >= LinkState.DetachPipe;
        private bool IsClosed => _senderLink.IsClosed;

        public Task ProduceAsync(Message message, CancellationToken cancellationToken = default)
        {
            if (_senderLink.IsDetaching() || _senderLink.IsClosed)
            {
                throw ProducerClosedException.BecauseProducerDetached();
            }

            cancellationToken.ThrowIfCancellationRequested();
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            cancellationToken.Register(() => tcs.SetCanceled());
            ProduceInternal(message, null, _onOutcome, tcs);
            return tcs.Task;
        }

        private static void OnOutcome(ILink sender, Amqp.Message message, Outcome outcome, object state)
        {
            var tcs = (TaskCompletionSource<bool>) state;
            var link = (Link) sender;
            if (outcome.Descriptor.Code == MessageOutcomes.Accepted.Descriptor.Code)
            {
                tcs.TrySetResult(true);
            }
            else if (link.IsDetaching() || link.IsClosed)
            {
                tcs.TrySetException(ProducerClosedException.BecauseProducerDetached());
            }
            else if (outcome.Descriptor.Code == MessageOutcomes.Rejected.Descriptor.Code)
            {
                tcs.TrySetException(MessageSendException.FromError(((Rejected) outcome).Error));
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
            ProduceInternal(message, null, null, null);
        }

        private void ProduceInternal(Message message, DeliveryState deliveryState, OutcomeCallback callback, object state)
        {
            try
            {
                _senderLink.Send(message.InnerMessage, deliveryState, callback, state);
            }
            catch (AmqpException e) when (IsClosed || IsDetaching)
            {
                throw ProducerClosedException.FromError(e.Error);
            }
            catch (AmqpException e)
            {
                throw MessageSendException.FromError(e.Error);
            }
            catch (Exception e)
            {
                throw MessageSendException.FromMessage(e.ToString());
            }
        }

        public async ValueTask DisposeAsync()
        {
            await _senderLink.CloseAsync().ConfigureAwait(false);
        }
    }
}