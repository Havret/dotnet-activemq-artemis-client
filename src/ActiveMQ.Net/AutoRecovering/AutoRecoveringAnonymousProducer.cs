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
            try
            {
                await _producer.SendAsync(address, routingType, message, cancellationToken).ConfigureAwait(false);
            }
            catch (ProducerClosedException)
            {
                Suspend();
                await WaitAsync(cancellationToken).ConfigureAwait(false);
                Log.RetryingSendAsync(Logger);
                await _producer.SendAsync(address, routingType, message, cancellationToken).ConfigureAwait(false);
            }
        }

        public void Send(string address, AddressRoutingType routingType, Message message)
        {
            try
            {
                _producer.Send(address, routingType, message);
            }
            catch (ProducerClosedException)
            {
                Suspend();
                Wait();
                Log.RetryingSendAsync(Logger);
                _producer.Send(address, routingType, message);
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