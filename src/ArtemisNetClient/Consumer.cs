using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Exceptions;
using ActiveMQ.Artemis.Client.Transactions;
using Amqp;
using Amqp.Framing;
using Amqp.Transactions;
using Microsoft.Extensions.Logging;

namespace ActiveMQ.Artemis.Client
{
    internal class Consumer : IConsumer
    {
        private readonly ILogger<Consumer> _logger;
        private readonly ReceiverLink _receiverLink;
        private readonly TransactionsManager _transactionsManager;
        private readonly ChannelReader<Message> _reader;
        private readonly ChannelWriter<Message> _writer;
        private readonly bool _isDurable;
        private bool _disposed;

        public Consumer(ILoggerFactory loggerFactory, ReceiverLink receiverLink, TransactionsManager transactionsManager, ConsumerConfiguration configuration)
        {
            _logger = loggerFactory.CreateLogger<Consumer>();
            _receiverLink = receiverLink;
            _transactionsManager = transactionsManager;
            _isDurable = configuration.Durable;
            var channel = Channel.CreateBounded<Message>(configuration.Credit);
            _reader = channel.Reader;
            _writer = channel.Writer;
            _receiverLink.Start(configuration.Credit, (_, m) =>
            {
                var message = new Message(m);
                if (_writer.TryWrite(message))
                {
                    Log.MessageBuffered(_logger, message, _receiverLink);
                }
                else
                {
                    Log.FailedToBufferMessage(_logger, message, _receiverLink);
                }
            });
            _receiverLink.Closed += OnClosed;
        }

        public async ValueTask<Message> ReceiveAsync(CancellationToken cancellationToken = default)
        {
            CheckState();

            try
            {
                Log.ReceivingMessage(_logger, _receiverLink);
                return await _reader.ReadAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (ChannelClosedException e) when (e.InnerException is ConsumerClosedException)
            {
                throw e.InnerException;
            }
            catch (ChannelClosedException e)
            {
                throw new ConsumerClosedException(e);
            }
        }

        public async ValueTask AcceptAsync(Message message, Transaction transaction, CancellationToken cancellationToken = default)
        {
            CheckState();
            
            var txnId = await _transactionsManager.GetTxnIdAsync(transaction, cancellationToken).ConfigureAwait(false);
            var deliveryState = txnId != null
                ? (DeliveryState) new TransactionalState { Outcome = new Accepted(), TxnId = txnId }
                : new Accepted();
            _receiverLink.Complete(message.InnerMessage, deliveryState);
            Log.MessageAccepted(_logger, message, _receiverLink);
        }

        public void Modify(Message message, bool deliveryFailed, bool undeliverableHere)
        {
            CheckState();

            _receiverLink.Modify(message.InnerMessage, deliveryFailed: deliveryFailed, undeliverableHere: undeliverableHere);
            Log.MessageModified(_logger, message, _receiverLink);
        }

        public void Reject(Message message)
        {
            CheckState();

            _receiverLink.Reject(message.InnerMessage);
            Log.MessageRejected(_logger, message, _receiverLink);
        }

        private void CheckState()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(Consumer));
            }
            
            if (_receiverLink.IsDetaching() || _receiverLink.IsClosed)
            {
                throw GetConsumerClosedException(_receiverLink.Error);
            }
        }
        
        private void OnClosed(IAmqpObject sender, Error error)
        {
            var consumerClosedException = GetConsumerClosedException(error);
            Log.ConsumerClosed(_logger, _receiverLink, consumerClosedException);
            _writer.TryComplete(consumerClosedException);
        }

        private static ConsumerClosedException GetConsumerClosedException(Error error)
        {
            return new ConsumerClosedException(error?.Description, error?.Condition);
        }

        public async ValueTask DisposeAsync()
        {
            _writer.TryComplete();
            if (_disposed)
            {
                return;
            }

            if (!_receiverLink.IsClosed)
            {
                _receiverLink.Closed -= OnClosed;
                if (_isDurable)
                {
                    await _receiverLink.DetachAsync().ConfigureAwait(false);    
                }
                else
                {
                    await _receiverLink.CloseAsync().ConfigureAwait(false);    
                }
            }

            if (!_receiverLink.Session.IsClosed)
            {
                await _receiverLink.Session.CloseAsync().ConfigureAwait(false);    
            }

            _disposed = true;
        }

        private static class Log
        {
            private static readonly Action<ILogger, object, string, Exception> _messageBuffered = LoggerMessage.Define<object, string>(
                LogLevel.Trace,
                0,
                "Message buffered. MessageId: '{0}' ConsumerName: '{1}'.");

            private static readonly Action<ILogger, object, string, Exception> _failedToBufferMessage = LoggerMessage.Define<object, string>(
                LogLevel.Warning,
                0,
                "Failed to buffer message. MessageId: '{0}' ConsumerName: '{1}'.");

            private static readonly Action<ILogger, string, Exception> _receivingMessage = LoggerMessage.Define<string>(
                LogLevel.Trace,
                0,
                "Receiving message. ConsumerName: '{0}'.");

            private static readonly Action<ILogger, object, string, Exception> _messageAccepted = LoggerMessage.Define<object, string>(
                LogLevel.Trace,
                0,
                "Message accepted. MessageId: '{0}' ConsumerName: '{1}'.");

            private static readonly Action<ILogger, object, string, Exception> _messageModified = LoggerMessage.Define<object, string>(
                LogLevel.Trace,
                0,
                "Message modified. MessageId: '{0}' ConsumerName: '{1}'.");

            private static readonly Action<ILogger, object, string, Exception> _messageRejected = LoggerMessage.Define<object, string>(
                LogLevel.Trace,
                0,
                "Message rejected. MessageId: '{0}' ConsumerName: '{1}'.");

            private static readonly Action<ILogger, string, Exception> _consumerClosed = LoggerMessage.Define<string>(
                LogLevel.Information,
                0,
                "Consumer closed. ConsumerName: '{0}'."
            );

            public static void MessageBuffered(ILogger logger, Message message, ReceiverLink receiverLink)
            {
                if (logger.IsEnabled(LogLevel.Trace))
                {
                    _messageBuffered(logger, message.GetMessageId<object>(), receiverLink.Name, null);
                }
            }

            public static void FailedToBufferMessage(ILogger logger, Message message, ReceiverLink receiverLink)
            {
                if (logger.IsEnabled(LogLevel.Warning))
                {
                    _failedToBufferMessage(logger, message.GetMessageId<object>(), receiverLink.Name, null);
                }
            }

            public static void ReceivingMessage(ILogger logger, ReceiverLink receiverLink)
            {
                if (logger.IsEnabled(LogLevel.Trace))
                {
                    _receivingMessage(logger, receiverLink.Name, null);
                }
            }

            public static void MessageAccepted(ILogger logger, Message message, ReceiverLink receiverLink)
            {
                if (logger.IsEnabled(LogLevel.Trace))
                {
                    _messageAccepted(logger, message.GetMessageId<object>(), receiverLink.Name, null);
                }
            }

            public static void MessageModified(ILogger logger, Message message, ReceiverLink receiverLink)
            {
                if (logger.IsEnabled(LogLevel.Trace))
                {
                    _messageModified(logger, message.GetMessageId<object>(), receiverLink.Name, null);
                }
            }

            public static void MessageRejected(ILogger logger, Message message, ReceiverLink receiverLink)
            {
                if (logger.IsEnabled(LogLevel.Trace))
                {
                    _messageRejected(logger, message.GetMessageId<object>(), receiverLink.Name, null);
                }
            }

            public static void ConsumerClosed(ILogger logger, ReceiverLink receiverLink, ConsumerClosedException exception)
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    _consumerClosed(logger, receiverLink.Name, exception);
                }
            }
        }
    }
}