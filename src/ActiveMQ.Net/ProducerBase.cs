using System;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Net.Exceptions;
using Amqp;
using Amqp.Framing;
using Microsoft.Extensions.Logging;

namespace ActiveMQ.Net
{
    internal abstract class ProducerBase
    {
        private static readonly OutcomeCallback _onOutcome = OnOutcome;

        private readonly ILogger _logger;
        private readonly SenderLink _senderLink;
        private readonly IBaseProducerConfiguration _configuration;

        protected ProducerBase(ILoggerFactory loggerFactory, SenderLink senderLink, IBaseProducerConfiguration configuration)
        {
            _logger = loggerFactory.CreateLogger(GetType());
            _senderLink = senderLink;
            _configuration = configuration;
        }

        private bool IsDetaching => _senderLink.LinkState >= LinkState.DetachPipe;
        private bool IsClosed => _senderLink.IsClosed;

        protected Task SendInternalAsync(string address, AddressRoutingType routingType, Message message, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            cancellationToken.Register(() => tcs.TrySetCanceled());
            Send(address, routingType, message, null, _onOutcome, tcs);
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

        protected void SendInternal(string address, AddressRoutingType routingType, Message message)
        {
            Send(address: address, routingType: routingType, message: message, deliveryState: null, callback: null, state: null);
        }

        private void Send(string address,
            AddressRoutingType routingType,
            Message message,
            DeliveryState deliveryState,
            OutcomeCallback callback,
            object state)
        {
            if (_senderLink.IsDetaching() || _senderLink.IsClosed)
            {
                throw ProducerClosedException.BecauseProducerDetached();
            }
            
            try
            {
                message.Priority ??= _configuration.MessagePriority;
                message.Properties.To = address;
                message.MessageAnnotations[SymbolUtils.RoutingType] = routingType.GetRoutingAnnotation();
                
                _senderLink.Send(message.InnerMessage, deliveryState, callback, state);
                Log.MessageSent(_logger);
            }
            catch (AmqpException e) when (IsClosed || IsDetaching)
            {
                throw ProducerClosedException.FromError(e.Error);
            }
            catch (AmqpException e)
            {
                throw MessageSendException.FromError(e.Error);
            }
            catch (ObjectDisposedException e)
            {
                throw ProducerClosedException.FromException(e);
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

        private static class Log
        {
            private static readonly Action<ILogger, Exception> _messageSent = LoggerMessage.Define(
                LogLevel.Trace,
                0,
                "Message sent.");

            public static void MessageSent(ILogger logger)
            {
                if (logger.IsEnabled(LogLevel.Trace))
                {
                    _messageSent(logger, null);
                }
            }
        }
    }
}