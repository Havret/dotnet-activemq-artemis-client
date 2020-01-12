using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ActiveMQ.Net.AutoRecovering
{
    internal class AutoRecoveringConsumer : IConsumer, IRecoverable
    {
        private readonly ILogger<AutoRecoveringConsumer> _logger;
        private readonly string _address;
        private readonly RoutingType _routingType;
        private IConsumer _consumer;

        public AutoRecoveringConsumer(ILoggerFactory loggerFactory, string address, RoutingType routingType)
        {
            _logger = loggerFactory.CreateLogger<AutoRecoveringConsumer>();
            _address = address;
            _routingType = routingType;
        }

        public async ValueTask<Message> ReceiveAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await _consumer.ReceiveAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (ChannelClosedException)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                Log.RetryingReceiveAsync(_logger);
                
                // TODO: Replace this naive retry logic with sth more sophisticated, e.g. using Polly
                return await ReceiveAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        public void Accept(Message message)
        {
            _consumer.Accept(message);
        }

        public void Reject(Message message)
        {
            _consumer.Reject(message);
        }
        
        public void Suspend()
        {
            // TODO: Implement
        }

        public async ValueTask DisposeAsync()
        {
            await _consumer.DisposeAsync().ConfigureAwait(false);
            Closed?.Invoke(this);
        }

        public async Task RecoverAsync(IConnection connection)
        {
            if (_consumer != null)
            {
                try
                {
                    await _consumer.DisposeAsync().ConfigureAwait(false);
                }
                catch (Exception)
                {
                    // ignored
                }
            }

            _consumer = await connection.CreateConsumerAsync(_address, _routingType).ConfigureAwait(false);
        }

        public event Closed Closed;

        private static class Log
        {
            private static readonly Action<ILogger, Exception> _retryingConsumeAsync = LoggerMessage.Define(
                LogLevel.Trace,
                0,
                "Retrying ReceiveAsync after Consumer reestablished.");
            
            public static void RetryingReceiveAsync(ILogger logger)
            {
                if (logger.IsEnabled(LogLevel.Trace))
                {
                    _retryingConsumeAsync(logger, null);    
                }
            }
        }
    }
}