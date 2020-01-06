using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ActiveMQ.Net.AutoRecovering
{
    internal class AutoRecoveringProducer : IProducer, IRecoverable
    {
        private readonly ILogger _logger;
        private readonly string _address;
        private readonly RoutingType _routingType;
        private IProducer _producer;

        public AutoRecoveringProducer(ILoggerFactory loggerFactory, string address, RoutingType routingType)
        {
            _logger = loggerFactory.CreateLogger<AutoRecoveringProducer>();
            _address = address;
            _routingType = routingType;
        }

        public async Task ProduceAsync(Message message, CancellationToken cancellationToken = default)
        {
            try
            {
                await _producer.ProduceAsync(message, cancellationToken).ConfigureAwait(false);
            }
            catch (ProducerClosedException)
            {
                // TODO: Replace this naive retry logic with sth more sophisticated, e.g. using Polly 
                Log.RetryingProduceAsync(_logger);

                await Task.Delay(100, cancellationToken).ConfigureAwait(false);
                await ProduceAsync(message, cancellationToken).ConfigureAwait(false);
            }
        }

        public void Produce(Message message)
        {
            try
            {
                _producer.Produce(message);
            }
            catch (ProducerClosedException)
            {
                // TODO: Replace this naive retry logic with sth more sophisticated, e.g. using Polly 
                Log.RetryingProduceAsync(_logger);

                Task.Delay(100).ConfigureAwait(false).GetAwaiter();
                Produce(message);
            }
        }

        public async ValueTask DisposeAsync()
        {
            await _producer.DisposeAsync().ConfigureAwait(false);
            Closed?.Invoke(this);
        }

        public async Task RecoverAsync(IConnection connection)
        {
            _producer = await connection.CreateProducer(_address, _routingType).ConfigureAwait(false);
        }

        public event Closed Closed;

        private static class Log
        {
            private static readonly Action<ILogger, Exception> _retryingProduceAsync = LoggerMessage.Define(
                LogLevel.Trace,
                0,
                "Retrying ProduceAsync after Producer reestablished.");

            public static void RetryingProduceAsync(ILogger logger)
            {
                if (logger.IsEnabled(LogLevel.Trace))
                {
                    _retryingProduceAsync(logger, null);
                }
            }
        }
    }
}