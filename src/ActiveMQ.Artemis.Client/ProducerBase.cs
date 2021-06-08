using System;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Exceptions;
using ActiveMQ.Artemis.Client.InternalUtilities;
using ActiveMQ.Artemis.Client.MessageIdPolicy;
using ActiveMQ.Artemis.Client.Transactions;
using Amqp;
using Amqp.Framing;
using Amqp.Transactions;
using Microsoft.Extensions.Logging;

namespace ActiveMQ.Artemis.Client
{
    internal abstract class ProducerBase
    {
        private static readonly OutcomeCallback _onOutcome = OnOutcome;

        private readonly ILogger _logger;
        private readonly SenderLink _senderLink;
        private readonly TransactionsManager _transactionsManager;
        private readonly IBaseProducerConfiguration _configuration;
        private bool _disposed;

        protected ProducerBase(ILoggerFactory loggerFactory, SenderLink senderLink, TransactionsManager transactionsManager, IBaseProducerConfiguration configuration)
        {
            _logger = loggerFactory.CreateLogger(GetType());
            _senderLink = senderLink;
            _transactionsManager = transactionsManager;
            _configuration = configuration;
        }

        private bool IsDetaching => _senderLink.LinkState >= LinkState.DetachPipe;
        private bool IsClosed => _senderLink.IsClosed;

        protected async Task SendInternalAsync(string address, RoutingType? routingType, Message message, Transaction transaction, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var txnId = await _transactionsManager.GetTxnIdAsync(transaction, cancellationToken).ConfigureAwait(false);
            var transactionalState = txnId != null ? new TransactionalState { TxnId = txnId } : null;
            var tcs = TaskUtil.CreateTaskCompletionSource<bool>(cancellationToken);
            cancellationToken.Register(() =>
            {
                if (tcs.TrySetCanceled())
                {
                    _senderLink.Cancel(message.InnerMessage);    
                }
            });
            message.DurabilityMode ??= _configuration.MessageDurabilityMode ?? DurabilityMode.Durable;
            Send(address, routingType, message, transactionalState, _onOutcome, tcs);
            await tcs.Task.ConfigureAwait(false);
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

        protected void SendInternal(string address, RoutingType? routingType, Message message)
        {
            message.DurabilityMode ??= _configuration.MessageDurabilityMode ?? DurabilityMode.Nondurable;
            Send(address: address, routingType: routingType, message: message, deliveryState: null, callback: null, state: null);
        }

        private void Send(string address,
            RoutingType? routingType,
            Message message,
            DeliveryState deliveryState,
            OutcomeCallback callback,
            object state)
        {
            CheckState();

            try
            {
                if (_configuration.SetMessageCreationTime && !message.CreationTime.HasValue)
                {
                    message.CreationTime = DateTime.UtcNow;
                }

                if (message.GetMessageId<object>() == null && _configuration.MessageIdPolicy != null && !(_configuration.MessageIdPolicy is DisableMessageIdPolicy))
                {
                    message.SetMessageId(_configuration.MessageIdPolicy.GetNextMessageId());
                }

                message.Priority ??= _configuration.MessagePriority;
                message.Properties.To = address;
                message.MessageAnnotations[SymbolUtils.RoutingType] ??= routingType.GetRoutingAnnotation();

                _senderLink.Send(message.InnerMessage, deliveryState, callback, state);
                Log.MessageSent(_logger, message);
            }
            catch (AmqpException e) when (IsClosed || IsDetaching)
            {
                throw new ProducerClosedException(e.Error.Description, e.Error.Condition, e);
            }
            catch (AmqpException e)
            {
                throw new MessageSendException(e.Error.Description, e.Error.Condition, e);
            }
            catch (ObjectDisposedException e)
            {
                throw new ProducerClosedException(e);
            }
            catch (Exception e)
            {
                throw new MessageSendException("Failed to send the message.", e);
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

            if (!_senderLink.IsClosed)
            {
                await _senderLink.CloseAsync().ConfigureAwait(false);                
            }

            if (!_senderLink.Session.IsClosed)
            {
                await _senderLink.Session.CloseAsync().ConfigureAwait(false);
            }

            _disposed = true;
        }

        private static class Log
        {
            private static readonly Action<ILogger, object, Exception> _messageSent = LoggerMessage.Define<object>(
                LogLevel.Trace,
                0,
                "Message sent. MessageId: '{0}'.");

            public static void MessageSent(ILogger logger, Message message)
            {
                if (logger.IsEnabled(LogLevel.Trace))
                {
                    _messageSent(logger, message.GetMessageId<object>(), null);
                }
            }
        }
    }
}