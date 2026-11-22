using System;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Exceptions;
using ActiveMQ.Artemis.Client.Transactions;
using Amqp;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;

namespace ActiveMQ.Artemis.Client.AutoRecovering
{
    internal class AutoRecoveringConsumer : IConsumer, IRecoverable
    {
        private readonly ILogger<AutoRecoveringConsumer> _logger;
        private readonly ConsumerConfiguration _configuration;
        private readonly AsyncManualResetEvent _manualResetEvent = new(true);

        // Makes replacing the consumer mutually exclusive with suspending on its failure, termination and disposal
        private readonly object _syncRoot = new();
        private volatile bool _closed;
        private volatile bool _disposed;
        private volatile Exception _failureCause;
        private volatile IConsumer _consumer;

        public AutoRecoveringConsumer(ILoggerFactory loggerFactory, ConsumerConfiguration configuration)
        {
            _logger = loggerFactory.CreateLogger<AutoRecoveringConsumer>();
            _configuration = configuration;
        }

        public async ValueTask<Message> ReceiveAsync(CancellationToken cancellationToken = default)
        {
            while (true)
            {
                CheckState();
                var consumer = _consumer;

                try
                {
                    return await consumer.ReceiveAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e) when (IsRecoverableFailure(e, consumer))
                {
                    CheckState();

                    Log.RetryingReceiveAsync(_logger);

                    RequestRecovery(consumer);
                    await _manualResetEvent.WaitAsync(cancellationToken).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Suspends the consumer and requests its recovery, unless the failed consumer has already been replaced.
        /// A late failure of a replaced consumer must not suspend its healthy replacement, as that would make it be
        /// replaced as well.
        /// </summary>
        private void RequestRecovery(IConsumer failedConsumer)
        {
            lock (_syncRoot)
            {
                if (_closed || _disposed || !ReferenceEquals(failedConsumer, _consumer))
                {
                    return;
                }

                Suspend();
            }

            RecoveryRequested?.Invoke();
        }

        /// <summary>
        /// Whether the consumer failed, because its link was lost, and is going to be recovered.
        /// ObjectDisposedException means the consumer was replaced and disposed by the recovery in the meantime.
        /// </summary>
        private bool IsRecoverableFailure(Exception exception, IConsumer consumer)
        {
            return exception switch
            {
                ConsumerClosedException { ErrorCode: ErrorCode.ResourceDeleted } => false,
                ConsumerClosedException => true,
                ObjectDisposedException => !ReferenceEquals(consumer, _consumer),
                _ => false
            };
        }

        public async ValueTask AcceptAsync(Message message, Transaction transaction, CancellationToken cancellationToken = default)
        {
            CheckState();
            var consumer = _consumer;

            try
            {
                await consumer.AcceptAsync(message, transaction, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (IsRecoverableFailure(e, consumer))
            {
                throw CreateSettlementException(e);
            }
        }

        public void Modify(Message message, bool deliveryFailed, bool undeliverableHere)
        {
            CheckState();
            var consumer = _consumer;

            try
            {
                consumer.Modify(message, deliveryFailed, undeliverableHere);
            }
            catch (Exception e) when (IsRecoverableFailure(e, consumer))
            {
                throw CreateSettlementException(e);
            }
        }

        public void Reject(Message message)
        {
            CheckState();
            var consumer = _consumer;

            try
            {
                consumer.Reject(message);
            }
            catch (Exception e) when (IsRecoverableFailure(e, consumer))
            {
                throw CreateSettlementException(e);
            }
        }

        /// <summary>
        /// The link the message was received on is gone, but the consumer is going to be recovered. Unless it was closed
        /// for good in the meantime, which is what the caller has to learn about.
        /// </summary>
        private Exception CreateSettlementException(Exception exception)
        {
            CheckState();
            return new MessageSettlementException(exception);
        }

        private void CheckState()
        {
            if (_closed)
            {
                if (_failureCause != null)
                {
                    throw new ConsumerClosedException("The Consumer was closed due to an unrecoverable error.", _failureCause);
                }
                else
                {
                    throw new ConsumerClosedException();
                }
            }

            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(AutoRecoveringConsumer));
            }
        }

        public void Suspend()
        {
            lock (_syncRoot)
            {
                // Nothing is going to resume it anymore, the receives waiting for it would wait forever
                if (_closed || _disposed)
                {
                    return;
                }

                var wasSuspended = IsSuspended;
                _manualResetEvent.Reset();

                if (!wasSuspended)
                {
                    Log.ConsumerSuspended(_logger);
                }
            }
        }

        public void Resume()
        {
            var wasSuspended = IsSuspended;
            _manualResetEvent.Set();

            if (wasSuspended)
            {
                Log.ConsumerResumed(_logger);
            }
        }

        public bool IsSuspended => !_manualResetEvent.IsSet;

        public async Task RecoverAsync(IConnection connection, CancellationToken cancellationToken)
        {
            var oldConsumer = _consumer;
            var newConsumer = await connection.CreateConsumerAsync(_configuration, cancellationToken).ConfigureAwait(false);
            lock (_syncRoot)
            {
                if (!_closed && !_disposed)
                {
                    _consumer = newConsumer;
                    newConsumer = null;
                }
            }

            if (newConsumer != null)
            {
                // Terminated or disposed in the meantime, nobody is going to use it
                await DisposeUnderlyingConsumerSafe(newConsumer).ConfigureAwait(false);
                return;
            }

            await DisposeUnderlyingConsumerSafe(oldConsumer).ConfigureAwait(false);
            if (oldConsumer == null)
            {
                Log.ConsumerCreated(_logger);
            }
            else
            {
                Log.ConsumerRecovered(_logger);
            }
        }

        public async Task TerminateAsync(Exception exception)
        {
            lock (_syncRoot)
            {
                _failureCause = exception;
                _closed = true;
            }

            _manualResetEvent.Set();
            Log.ConsumerTerminated(_logger, exception);
            await DisposeUnderlyingConsumerSafe(_consumer).ConfigureAwait(false);
            Closed?.Invoke(this);
        }

        public async ValueTask DisposeAsync()
        {
            lock (_syncRoot)
            {
                _disposed = true;
            }

            // Wake up the receives waiting for the recovery, it's not going to happen anymore
            _manualResetEvent.Set();
            await DisposeUnderlyingConsumer(_consumer).ConfigureAwait(false);
            Closed?.Invoke(this);
        }

        private async Task DisposeUnderlyingConsumerSafe(IConsumer consumer)
        {
            try
            {
                await DisposeUnderlyingConsumer(consumer).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private static async ValueTask DisposeUnderlyingConsumer(IConsumer consumer)
        {
            if (consumer != null)
            {
                await consumer.DisposeAsync().ConfigureAwait(false);
            }
        }

        public event Closed Closed;

        public event RecoveryRequested RecoveryRequested;

        private static class Log
        {
            private static readonly Action<ILogger, Exception> _retryingConsumeAsync = LoggerMessage.Define(
                LogLevel.Warning,
                0,
                "Retrying receive after Consumer reestablished.");

            private static readonly Action<ILogger, Exception> _consumerCreated = LoggerMessage.Define(
                LogLevel.Information,
                0,
                "Consumer created.");

            private static readonly Action<ILogger, Exception> _consumerRecovered = LoggerMessage.Define(
                LogLevel.Information,
                0,
                "Consumer recovered.");
            
            private static readonly Action<ILogger, Exception> _consumerSuspended = LoggerMessage.Define(
                LogLevel.Warning,
                0,
                "Consumer suspended.");
            
            private static readonly Action<ILogger, Exception> _consumerResumed = LoggerMessage.Define(
                LogLevel.Information,
                0,
                "Consumer resumed.");
            
            private static readonly Action<ILogger, Exception> _consumerTerminated = LoggerMessage.Define(
                LogLevel.Error,
                0,
                "Consumer terminated.");
            
            public static void RetryingReceiveAsync(ILogger logger)
            {
                if (logger.IsEnabled(LogLevel.Warning))
                {
                    _retryingConsumeAsync(logger, null);    
                }
            }
            
            public static void ConsumerCreated(ILogger logger)
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    _consumerCreated(logger, null);
                }
            }

            public static void ConsumerRecovered(ILogger logger)
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    _consumerRecovered(logger, null);
                }
            }
            
            public static void ConsumerSuspended(ILogger logger)
            {
                if (logger.IsEnabled(LogLevel.Warning))
                {
                    _consumerSuspended(logger, null);
                }
            }
            
            public static void ConsumerResumed(ILogger logger)
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    _consumerResumed(logger, null);
                }
            }
            
            public static void ConsumerTerminated(ILogger logger, Exception exception)
            {
                if (logger.IsEnabled(LogLevel.Error))
                {
                    _consumerTerminated(logger, exception);
                }
            }
        }
    }
}