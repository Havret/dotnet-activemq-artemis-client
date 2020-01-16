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
                await _manualResetEvent.WaitAsync(cancellationToken).ConfigureAwait(false);
                await SendAsync(message, cancellationToken).ConfigureAwait(false);
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
                _manualResetEvent.Wait();
                Send(message);
            }
        }

        public async ValueTask DisposeAsync()
        {
            await _producer.DisposeAsync().ConfigureAwait(false);
            Closed?.Invoke(this);
        }

        public async Task RecoverAsync(IConnection connection)
        {
            _producer = await connection.CreateProducerAsync(_address, _routingType).ConfigureAwait(false);
            _manualResetEvent.Set();
        }

        public event Closed Closed;
        public void Suspend()
        {
            _manualResetEvent.Reset();
        }

        private static class Log
        {
            private static readonly Action<ILogger, Exception> _retryingProduceAsync = LoggerMessage.Define(
                LogLevel.Trace,
                0,
                "Retrying send after Producer reestablished.");

            public static void RetryingSendAsync(ILogger logger)
            {
                if (logger.IsEnabled(LogLevel.Trace))
                {
                    _retryingProduceAsync(logger, null);
                }
            }
        }
    }
}