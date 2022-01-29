﻿using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Exceptions;
using ActiveMQ.Artemis.Client.InternalUtilities;
using Amqp;
using Amqp.Framing;

namespace ActiveMQ.Artemis.Client
{
    internal class RpcClient : IRpcClient
    {
        private static readonly OutcomeCallback _onOutcome = OnOutcome;
        
        private readonly SenderLink _senderLink;
        private readonly ReceiverLink _receiverLink;
        private readonly string _replyToAddress;
        private readonly ConcurrentDictionary<string, TaskCompletionSource<Message>> _pendingRequests = new();
        private bool _disposed;

        public RpcClient(SenderLink senderLink, ReceiverLink receiverLink, string replyToAddress)
        {
            _senderLink = senderLink;
            _receiverLink = receiverLink;
            _replyToAddress = replyToAddress;

            _receiverLink.Start(200, (_, msg) =>
            {
                var message = new Message(msg);
                var correlationId = message.CorrelationId;
                if (_pendingRequests.TryGetValue(correlationId, out var tcs))
                {
                    tcs.TrySetResult(message);
                }
                _receiverLink.Accept(msg);
            });
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
            message.Properties.ReplyTo = _replyToAddress;
            message.MessageAnnotations[SymbolUtils.RoutingType] ??= routingType.GetRoutingAnnotation();

            var (tcs, ctr) = TaskUtil.CreateTaskCompletionSource<Message>(ref cancellationToken, () =>
            {
                _senderLink.Cancel(message.InnerMessage);
            });
            using var cancellationTokenRegistration = ctr;
            try
            {
                _pendingRequests.TryAdd(correlationId, tcs);
                _senderLink.Send(message.InnerMessage, null, _onOutcome, tcs);
                var response = await tcs.Task.ConfigureAwait(false);
                return response;
            }
            catch (AmqpException e) when (IsClosed || IsDetaching)
            {
                throw new RpcClientClosedException(e.Error.Description, e.Error.Condition, e);
            }
            catch (AmqpException e)
            {
                throw new MessageSendException(e.Error.Description, e.Error.Condition, e);
            }
            catch (ObjectDisposedException e)
            {
                throw new RpcClientClosedException(e);
            }
            catch (Exception e)
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
                tcs.TrySetException(new ProducerClosedException());
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
                throw new ObjectDisposedException(nameof(ProducerBase));
            }
            if (_senderLink.IsDetaching() || _senderLink.IsClosed)
            {
                throw new ProducerClosedException();
            }
        }
        
        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }
            
            await ActionUtil.ExecuteAll(
                () => _receiverLink.CloseAsync(),
                () => _senderLink.CloseAsync(),
                () => _senderLink.Session.CloseAsync()
            ).ConfigureAwait(false);
            
            _disposed = true;
        }
    }
}