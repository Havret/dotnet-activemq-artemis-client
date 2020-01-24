using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;

namespace ActiveMQ.Net.AutoRecovering
{
    internal class AutoRecoveringProducer : IProducer, IRecoverable
    {
        private readonly ILogger _logger;
        private readonly string _address;
        private readonly RoutingType _routingType;
        private readonly AsyncManualResetEvent _manualResetEvent = new AsyncManualResetEvent(true);
        private IProducer _producer;

        public AutoRecoveringProducer(ILoggerFactory loggerFactory, string address, RoutingType routingType)
        {
            _logger = loggerFactory.CreateLogger<AutoRecoveringProducer>();
            _address = address;
            _routingType = routingType;
        }

        public async Task SendAsync(Message message, CancellationToken cancellationToken = default)
        {
            try
            {
                await _producer.SendAsync(message, cancellationToken).ConfigureAwait(false);
            }
            catch (ProducerClosedException)
            {
                Log.RetryingSendAsync(_logger);
                
                Suspend();
                RecoveryRequested?.Invoke();
                await _manualResetEvent.WaitAsync(cancellationToken).ConfigureAwait(false);
                await _producer.SendAsync(message, cancellationToken).ConfigureAwait(false);
            }
        }

        public void Send(Message message)
        {
            try
            {
                _producer.Send(message);
            }
            catch (ProducerClosedException)
            {
                Log.RetryingSendAsync(_logger);
                
                Suspend();
                RecoveryRequested?.Invoke();
                _manualResetEvent.Wait();
                _producer.Send(message);
            }
        }

        public async ValueTask DisposeAsync()
        {
            await _producer.DisposeAsync().ConfigureAwait(false);
            Closed?.Invoke(this);
        }

        public async Task RecoverAsync(IConnection connection, CancellationToken cancellationToken)
        {
            _producer = await connection.CreateProducerAsync(_address, _routingType, cancellationToken).ConfigureAwait(false);
            Log.ProducerRecovered(_logger);
        }

        public void Resume()
        {
            _manualResetEvent.Set();
            Log.ProducerResumed(_logger);
        }

        public void Suspend()
        {
            _manualResetEvent.Reset();
            Log.ProducerSuspended(_logger);
        }

        public event Closed Closed;
        public event RecoveryRequested RecoveryRequested;

        private static class Log
        {
            private static readonly Action<ILogger, Exception> _retryingProduceAsync = LoggerMessage.Define(
                LogLevel.Trace,
                0,
                "Retrying send after Producer reestablished.");
            
            private static readonly Action<ILogger, Exception> _producerRecovered = LoggerMessage.Define(
                LogLevel.Trace,
                0,
                "Producer recovered.");
            
            private static readonly Action<ILogger, Exception> _producerSuspended = LoggerMessage.Define(
                LogLevel.Trace,
                0,
                "Producer suspended.");
            
            private static readonly Action<ILogger, Exception> _producerResumed = LoggerMessage.Define(
                LogLevel.Trace,
                0,
                "Producer resumed.");

            public static void RetryingSendAsync(ILogger logger)
            {
                if (logger.IsEnabled(LogLevel.Trace))
                {
                    _retryingProduceAsync(logger, null);
                }
            }
            
            public static void ProducerRecovered(ILogger logger)
            {
                if (logger.IsEnabled(LogLevel.Trace))
                {
                    _producerRecovered(logger, null);
                }
            }
            
            public static void ProducerSuspended(ILogger logger)
            {
                if (logger.IsEnabled(LogLevel.Trace))
                {
                    _producerSuspended(logger, null);
                }
            }
            
            public static void ProducerResumed(ILogger logger)
            {
                if (logger.IsEnabled(LogLevel.Trace))
                {
                    _producerResumed(logger, null);
                }
            }
        }
    }
}