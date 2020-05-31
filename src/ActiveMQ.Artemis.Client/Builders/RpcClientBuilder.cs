using System;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Exceptions;
using ActiveMQ.Artemis.Client.InternalUtilities;
using ActiveMQ.Artemis.Client.Management;
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

        public async Task<RpcClient> CreateAsync(string address, CancellationToken cancellationToken)
        {
            var senderLink = await CreateSenderLink(address, cancellationToken).ConfigureAwait(false);
            var (receiverLink, replyToAddress) = await CreateReceiverLink(cancellationToken).ConfigureAwait(false);
            return new RpcClient(senderLink, receiverLink, replyToAddress);
        }

        private async Task<SenderLink> CreateSenderLink(string address, CancellationToken cancellationToken)
        {
            var tcs = TaskUtil.CreateTaskCompletionSource<bool>(cancellationToken);
            var senderLink = new SenderLink(_session, Guid.NewGuid().ToString(), new Target
            {
                Address = address
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
            var tcs = TaskUtil.CreateTaskCompletionSource<string>(cancellationToken);
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
                if (attach != null && attach.Source is Source source)
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