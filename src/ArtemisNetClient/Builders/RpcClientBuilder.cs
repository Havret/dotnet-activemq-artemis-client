using System;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Exceptions;
using ActiveMQ.Artemis.Client.InternalUtilities;
using Amqp;
using Amqp.Framing;

namespace ActiveMQ.Artemis.Client.Builders
{
    internal class RpcClientBuilder
    {
        private readonly Session _session;

        public RpcClientBuilder(Session session)
        {
            _session = session;
        }

        public async Task<RequestReplyClient> CreateAsync(CancellationToken cancellationToken)
        {
            var senderLink = await CreateSenderLink(cancellationToken).ConfigureAwait(false);
            var (receiverLink, replyToAddress) = await CreateReceiverLink(cancellationToken).ConfigureAwait(false);
            return new RequestReplyClient(senderLink, receiverLink, replyToAddress);
        }

        private async Task<SenderLink> CreateSenderLink(CancellationToken cancellationToken)
        {
            var (tcs, ctr) = TaskUtil.CreateTaskCompletionSource<bool>(ref cancellationToken);
            using var _ = ctr;
            var senderLink = new SenderLink(_session, Guid.NewGuid().ToString(), new Target
            {
                Address = null
            }, OnAttached);
            senderLink.AddClosedCallback(OnClosed);
            await tcs.Task.ConfigureAwait(false);
            senderLink.Closed -= OnClosed;
            return senderLink;

            void OnAttached(ILink link, Attach attach)
            {
                if (attach != null)
                {
                    tcs.TrySetResult(true);
                }
            }

            void OnClosed(IAmqpObject sender, Error error)
            {
                if (error != null)
                {
                    tcs.TrySetException(new CreateRpcClientException(error.Description, error.Condition));
                }
            }
        }

        private async Task<(ReceiverLink receiverLink, string address)> CreateReceiverLink(CancellationToken cancellationToken)
        {
            var (tcs, ctr) = TaskUtil.CreateTaskCompletionSource<string>(ref cancellationToken);
            using var _ = ctr;
            var receiverLink = new ReceiverLink(_session, Guid.NewGuid().ToString(), new Source
            {
                Dynamic = true
            }, OnAttached);
            receiverLink.AddClosedCallback(OnClosed);
            var address = await tcs.Task.ConfigureAwait(false);
            receiverLink.Closed -= OnClosed;
            return (receiverLink, address);

            void OnAttached(ILink link, Attach attach)
            {
                if (attach is { Source: Source source })
                {
                    tcs.TrySetResult(source.Address);
                }
            }

            void OnClosed(IAmqpObject sender, Error error)
            {
                if (error != null)
                {
                    tcs.TrySetException(new CreateRpcClientException(error.Description, error.Condition));
                }
            }
        }
    }
}