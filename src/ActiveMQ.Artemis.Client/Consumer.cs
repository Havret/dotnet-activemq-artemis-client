using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
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

        public Consumer(ILoggerFactory loggerFactory, ReceiverLink receiverLink, TransactionsManager transactionsManager, ConsumerConfiguration configuration)
        {
            _logger = loggerFactory.CreateLogger<Consumer>();
            _receiverLink = receiverLink;
            _transactionsManager = transactionsManager;
            var channel = Channel.CreateBounded<Message>(configuration.Credit);
            _reader = channel.Reader;
            _writer = channel.Writer;
            _receiverLink.Start(configuration.Credit, (receiver, m) =>
            {
                var message = new Message(m);
                if (_writer.TryWrite(message))
                {
                    Log.MessageBuffered(_logger, message);
                }
                else
                {
                    Log.FailedToBufferMessage(_logger, message);
                }
            });
        }

        public async ValueTask<Message> ReceiveAsync(CancellationToken cancellationToken = default)
        {
            Log.ReceivingMessage(_logger);
            return await _reader.ReadAsync(cancellationToken);
        }

        public async ValueTask AcceptAsync(Message message, Transaction transaction, CancellationToken cancellationToken = default)
        {
            var txnId = await _transactionsManager.GetTxnIdAsync(transaction, cancellationToken).ConfigureAwait(false);
            var deliveryState = txnId != null
                ? (DeliveryState) new TransactionalState { Outcome = new Accepted(), TxnId = txnId }
                : new Accepted();
            _receiverLink.Complete(message.InnerMessage, deliveryState);
            Log.MessageAccepted(_logger, message);
        }

        public void Reject(Message message, bool undeliverableHere)
        {
            _receiverLink.Modify(message.InnerMessage, deliveryFailed: true, undeliverableHere: undeliverableHere);
            Log.MessageRejected(_logger, message);
        }

        public async ValueTask DisposeAsync()
        {
            _writer.TryComplete();
            if (!_receiverLink.IsClosed)
            {
                await _receiverLink.CloseAsync().ConfigureAwait(false);
            }
        }

        private static class Log
        {
            private static readonly Action<ILogger, object, Exception> _messageBuffered = LoggerMessage.Define<object>(
                LogLevel.Trace,
                0,
                "Message buffered. MessageId: '{0}'.");

            private static readonly Action<ILogger, object, Exception> _failedToBufferMessage = LoggerMessage.Define<object>(
                LogLevel.Warning,
                0,
                "Failed to buffer message. MessageId: '{0}'.");

            private static readonly Action<ILogger, Exception> _receivingMessage = LoggerMessage.Define(
                LogLevel.Trace,
                0,
                "Receiving message.");

            private static readonly Action<ILogger, object, Exception> _messageAccepted = LoggerMessage.Define<object>(
                LogLevel.Trace,
                0,
                "Message accepted. MessageId: '{0}'.");

            private static readonly Action<ILogger, object, Exception> _messageRejected = LoggerMessage.Define<object>(
                LogLevel.Trace,
                0,
                "Message rejected. MessageId: '{0}'.");

            public static void MessageBuffered(ILogger logger, Message message)
            {
                if (logger.IsEnabled(LogLevel.Trace))
                {
                    _messageBuffered(logger, message.GetMessageId<object>(), null);
                }
            }

            public static void FailedToBufferMessage(ILogger logger, Message message)
            {
                if (logger.IsEnabled(LogLevel.Warning))
                {
                    _failedToBufferMessage(logger, message.GetMessageId<object>(), null);
                }
            }

            public static void ReceivingMessage(ILogger logger)
            {
                if (logger.IsEnabled(LogLevel.Trace))
                {
                    _receivingMessage(logger, null);
                }
            }

            public static void MessageAccepted(ILogger logger, Message message)
            {
                if (logger.IsEnabled(LogLevel.Trace))
                {
                    _messageAccepted(logger, message.GetMessageId<object>(), null);
                }
            }

            public static void MessageRejected(ILogger logger, Message message)
            {
                if (logger.IsEnabled(LogLevel.Trace))
                {
                    _messageRejected(logger, message.GetMessageId<object>(), null);
                }
            }
        }
    }
}