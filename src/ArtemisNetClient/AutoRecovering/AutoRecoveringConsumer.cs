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
        private bool _closed;
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

                try
                {
                    return await _consumer.ReceiveAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (ConsumerClosedException exception) when (IsRecoverable(exception))
                {
                    CheckState();

                    Log.RetryingReceiveAsync(_logger);

                    Suspend();
                    RecoveryRequested?.Invoke();
                    await _manualResetEvent.WaitAsync(cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private static bool IsRecoverable(ConsumerClosedException exception)
        {
            return exception.ErrorCode switch
            {
                ErrorCode.ResourceDeleted => false,
                _ => true
            };
        }

        public ValueTask AcceptAsync(Message message, Transaction transaction, CancellationToken cancellationToken = default)
        {
            CheckState();

            return _consumer.AcceptAsync(message, transaction, cancellationToken);
        }

        public void Reject(Message message, bool undeliverableHere)
        {
            CheckState();

            _consumer.Reject(message, undeliverableHere);
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
        }

        public void Suspend()
        {
            var wasSuspended = IsSuspended();
            _manualResetEvent.Reset();

            if (!wasSuspended)
            {
                Log.ConsumerSuspended(_logger);
            }
        }

        public void Resume()
        {
            var wasSuspended = IsSuspended();
            _manualResetEvent.Set();

            if (wasSuspended)
            {
                Log.ConsumerResumed(_logger);
            }
        }

        private bool IsSuspended()
        {
            return !_manualResetEvent.IsSet;
        }

        public async Task RecoverAsync(IConnection connection, CancellationToken cancellationToken)
        {
            var oldConsumer = _consumer;
            _consumer = await connection.CreateConsumerAsync(_configuration, cancellationToken).ConfigureAwait(false);
            await DisposeUnderlyingConsumerSafe(oldConsumer).ConfigureAwait(false);
            Log.ConsumerRecovered(_logger);
        }

        public async Task TerminateAsync(Exception exception)
        {
            _closed = true;
            _failureCause = exception;
            _manualResetEvent.Set();
            Log.ConsumerTerminated(_logger, exception);
            await DisposeUnderlyingConsumerSafe(_consumer).ConfigureAwait(false);
        }

        public async ValueTask DisposeAsync()
        {
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