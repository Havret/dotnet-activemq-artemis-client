using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;

namespace ActiveMQ.Net.AutoRecovering
{
    internal class AutoRecoveringConsumer : IConsumer, IRecoverable
    {
        private readonly ILogger<AutoRecoveringConsumer> _logger;
        private readonly string _address;
        private readonly RoutingType _routingType;
        private readonly AsyncManualResetEvent _manualResetEvent = new AsyncManualResetEvent(true);
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
                Log.RetryingReceiveAsync(_logger);
                
                await _manualResetEvent.WaitAsync(cancellationToken).ConfigureAwait(false);
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
            _manualResetEvent.Reset();
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
            _manualResetEvent.Set();
        }

        public event Closed Closed;

        private static class Log
        {
            private static readonly Action<ILogger, Exception> _retryingConsumeAsync = LoggerMessage.Define(
                LogLevel.Trace,
                0,
                "Retrying receive after Consumer reestablished.");
            
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