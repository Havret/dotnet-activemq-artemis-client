using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Exceptions;
using ActiveMQ.Artemis.Client.InternalUtilities;
using Amqp;
using Amqp.Framing;
using Microsoft.Extensions.Logging;

namespace ActiveMQ.Artemis.Client
{
    internal class RequestReplyClient : IRequestReplyClient
    {
        private static readonly OutcomeCallback _onOutcome = OnOutcome;

        private readonly ILogger _logger;
        private readonly SenderLink _senderLink;
        private readonly ReceiverLink _receiverLink;
        private readonly RequestReplyClientConfiguration _configuration;
        private readonly ConcurrentDictionary<string, TaskCompletionSource<Message>> _pendingRequests = new();
        private bool _disposed;

        public RequestReplyClient(ILoggerFactory loggerFactory,
            SenderLink senderLink,
            ReceiverLink receiverLink,
            RequestReplyClientConfiguration configuration)
        {
            _logger = loggerFactory.CreateLogger<RequestReplyClient>();
            _senderLink = senderLink;
            _receiverLink = receiverLink;
            _configuration = configuration;

            _receiverLink.Start(_configuration.Credit, (_, msg) =>
            {
                try
                {
                    _receiverLink.Accept(msg);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Failed acknowledge reply message.");
                }

                try
                {
                    var message = new Message(msg);
                    var correlationId = message.CorrelationId;
                    if (_pendingRequests.TryGetValue(correlationId, out var tcs))
                    {
                        tcs.TrySetResult(message);
                    }
                    else
                    {
                        _logger.LogWarning("The response could not be matched with any of the pending requests.");
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "The response could not be matched with any of the pending requests.");
                }
            });
            _receiverLink.AddClosedCallback(OnReceiverLinkClosed);
        }

        private void OnReceiverLinkClosed(IAmqpObject sender, Error error)
        {
            // The reply-to address doesn't outlive the receiver link, so pending requests would never get a reply
            FailPendingRequests(error);
        }

        private void FailPendingRequests(Error error)
        {
            foreach (var pendingRequest in _pendingRequests.Values)
            {
                pendingRequest.TrySetException(CreateClosedException(error));
            }
        }
        
        private bool IsDetaching => _senderLink.IsDetaching() || _receiverLink.IsDetaching();
        private bool IsClosed => _senderLink.IsClosed || _receiverLink.IsClosed;
        
        public async Task<Message> SendAsync(string address, RoutingType? routingType, Message message, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CheckState();

            var correlationId = Guid.NewGuid().ToString();
            message.CorrelationId = correlationId;
            message.Properties.To = address;
            message.Properties.ReplyTo = _configuration.ReplyToAddress;
            message.MessageAnnotations[SymbolUtils.RoutingType] ??= routingType.GetRoutingAnnotation();

            var (tcs, ctr) = TaskUtil.CreateTaskCompletionSource<Message>(ref cancellationToken, () =>
            {
                _senderLink.Cancel(message.InnerMessage);
            });
            using var cancellationTokenRegistration = ctr;
            try
            {
                _pendingRequests.TryAdd(correlationId, tcs);

                // The receiver link may have been closed after the first check, before the request was registered,
                // in which case nothing is going to fail it
                CheckState();

                _senderLink.Send(message.InnerMessage, null, _onOutcome, tcs);
                try
                {
                    return await tcs.Task.ConfigureAwait(false);
                }
                catch (RequestReplyClientClosedException e)
                {
                    // Failed only once sent, it may have been processed
                    e.RequestSent = true;
                    throw;
                }
            }
            catch (AmqpException e) when (IsClosed || IsDetaching)
            {
                throw new RequestReplyClientClosedException(e.Error.Description, e.Error.Condition, e);
            }
            catch (AmqpException e)
            {
                throw new MessageSendException(e.Error.Description, e.Error.Condition, e);
            }
            catch (ObjectDisposedException e)
            {
                throw new RequestReplyClientClosedException(e);
            }
            catch (Exception e) when (e is not OperationCanceledException and not ActiveMQArtemisClientException)
            {
                throw new MessageSendException("Failed to send the message.", e);
            }
            finally
            {
                _pendingRequests.TryRemove(correlationId, out _);
            }
        }
        
        private static void OnOutcome(ILink sender, Amqp.Message message, Outcome outcome, object state)
        {
            var tcs = (TaskCompletionSource<Message>) state;
            var link = (Link) sender;
            if (outcome.Descriptor.Code == MessageOutcomes.Accepted.Descriptor.Code)
            {
                // If the message has been accepted we should wait for the reply
            }
            else if (link.IsDetaching() || link.IsClosed)
            {
                tcs.TrySetException(CreateClosedException(link.Error));
            }
            else if (outcome.Descriptor.Code == MessageOutcomes.Rejected.Descriptor.Code)
            {
                var rejected = (Rejected) outcome;
                tcs.TrySetException(new MessageSendException(rejected.Error.Description, rejected.Error.Condition));
            }
            else if (outcome.Descriptor.Code == MessageOutcomes.Released.Descriptor.Code)
            {
                tcs.TrySetException(new MessageSendException("Message was released by remote peer.", ErrorCode.MessageReleased));
            }
            else
            {
                tcs.TrySetException(new MessageSendException(outcome.ToString(), ErrorCode.InternalError));
            }
        }
        
        private void CheckState()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(RequestReplyClient));
            }
            if (IsClosed || IsDetaching)
            {
                throw CreateClosedException(_senderLink.Error ?? _receiverLink.Error);
            }
        }

        private static RequestReplyClientClosedException CreateClosedException(Error error)
        {
            return error != null
                ? new RequestReplyClientClosedException(error.Description, error.Condition, null)
                : new RequestReplyClientClosedException();
        }
        
        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            try
            {
                await ActionUtil.ExecuteAll(
                    () => CloseIfOpen(_receiverLink),
                    () => CloseIfOpen(_senderLink),
                    () => CloseIfOpen(_senderLink.Session)
                ).ConfigureAwait(false);
            }
            finally
            {
                _disposed = true;
                FailPendingRequests(null);
            }
        }

        private async Task CloseIfOpen(AmqpObject amqpObject)
        {
            // Already closed when the connection was lost
            if (amqpObject.IsClosed)
            {
                return;
            }

            try
            {
                await amqpObject.CloseAsync().ConfigureAwait(false);
            }
            catch (AmqpException) when (_senderLink.Session.Connection.ConnectionState != ConnectionState.Opened)
            {
                // The connection was lost while closing, there is nothing left to close
            }
        }
    }
}