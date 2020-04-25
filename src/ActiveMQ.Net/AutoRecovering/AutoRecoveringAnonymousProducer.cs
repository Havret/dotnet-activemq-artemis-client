using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Net.Exceptions;
using Microsoft.Extensions.Logging;

namespace ActiveMQ.Net.AutoRecovering
{
    internal class AutoRecoveringAnonymousProducer : AutoRecoveringProducerBase, IAnonymousProducer
    {
        private IAnonymousProducer _producer;

        public AutoRecoveringAnonymousProducer(ILoggerFactory loggerFactory) : base(loggerFactory)
        {
        }
        
        public async Task SendAsync(string address, AddressRoutingType routingType, Message message, CancellationToken cancellationToken = default)
        {
            while (true)
            {
                try
                {
                    await _producer.SendAsync(address, routingType, message, cancellationToken).ConfigureAwait(false);
                    return;
                }
                catch (ProducerClosedException)
                {
                    HandleProducerClosed();
                    await WaitAsync(cancellationToken).ConfigureAwait(false);
                    Log.RetryingSendAsync(Logger);
                }
            }
        }

        public void Send(string address, AddressRoutingType routingType, Message message)
        {
            while (true)
            {
                try
                {
                    _producer.Send(address, routingType, message);
                    return;
                }
                catch (ProducerClosedException)
                {
                    HandleProducerClosed();
                    Wait();
                    Log.RetryingSendAsync(Logger);
                }
            }
        }

        protected override async Task RecoverUnderlyingProducer(IConnection connection, CancellationToken cancellationToken)
        {
            _producer = await connection.CreateAnonymousProducer(cancellationToken).ConfigureAwait(false);
        }

        protected override ValueTask DisposeUnderlyingProducer()
        {
            return _producer.DisposeAsync();
        }
    }
}