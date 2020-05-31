using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Exceptions;
using ActiveMQ.Artemis.Client.InternalUtilities;
using Amqp;
using Amqp.Framing;

namespace ActiveMQ.Artemis.Client.Management
{
    internal class RpcClient : IAsyncDisposable
    {
        private static readonly OutcomeCallback _onOutcome = OnOutcome;
        
        private readonly SenderLink _senderLink;
        private readonly ReceiverLink _receiverLink;
        private readonly string _replyToAddress;
        private readonly ConcurrentDictionary<Guid, TaskCompletionSource<Message>> _pendingRequests = new ConcurrentDictionary<Guid, TaskCompletionSource<Message>>();

        public RpcClient(SenderLink senderLink, ReceiverLink receiverLink, string replyToAddress)
        {
            _senderLink = senderLink;
            _receiverLink = receiverLink;
            _replyToAddress = replyToAddress;

            _receiverLink.Start(200, (receiver, msg) =>
            {
                var message = new Message(msg);
                var correlationId = message.GetCorrelationId<Guid>();
                if (_pendingRequests.TryGetValue(correlationId, out var tcs))
                {
                    tcs.TrySetResult(message);
                    receiver.Accept(msg);
                }
            });
        }
        
        public async Task<Message> SendAsync(Message message, CancellationToken cancellationToken)
        {
            var correlationId = Guid.NewGuid();
            message.SetCorrelationId(correlationId);
            message.Properties.ReplyTo = _replyToAddress;

            var tcs = TaskUtil.CreateTaskCompletionSource<Message>(cancellationToken);
            try
            {
                _pendingRequests.TryAdd(correlationId, tcs);
                _senderLink.Send(message.InnerMessage, null, _onOutcome, tcs);
                return await tcs.Task.ConfigureAwait(false);
            }
            finally
            {
                _pendingRequests.TryRemove(correlationId, out _);
            }
        }

        private static void OnOutcome(ILink sender, Amqp.Message message, Outcome outcome, object state)
        {
            var tcs = (TaskCompletionSource<Message>) state;
            if (outcome.Descriptor.Code != MessageOutcomes.Accepted.Descriptor.Code)
            {
                tcs.TrySetException(new MessageSendException(outcome.ToString(), ErrorCode.InternalError));
            }
        }

        public ValueTask DisposeAsync()
        {
            return DisposeUtil.DisposeAll(_receiverLink, _senderLink);
        }
    }
}