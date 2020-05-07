using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Net.Exceptions;
using ActiveMQ.Net.Transactions;
using Microsoft.Extensions.Logging;

namespace ActiveMQ.Net.AutoRecovering
{
    internal class AutoRecoveringAnonymousProducer : AutoRecoveringProducerBase, IAnonymousProducer
    {
        private readonly AnonymousProducerConfiguration _configuration;
        private IAnonymousProducer _producer;

        public AutoRecoveringAnonymousProducer(ILoggerFactory loggerFactory, AnonymousProducerConfiguration configuration) : base(loggerFactory)
        {
            _configuration = configuration;
        }
        
        public async Task SendAsync(string address, AddressRoutingType routingType, Message message, Transaction transaction, CancellationToken cancellationToken = default)
        {
            while (true)
            {
                try
                {
                    await _producer.SendAsync(address, routingType, message, transaction, cancellationToken).ConfigureAwait(false);
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
            _producer = await connection.CreateAnonymousProducer(_configuration, cancellationToken).ConfigureAwait(false);
        }

        protected override ValueTask DisposeUnderlyingProducer()
        {
            return _producer.DisposeAsync();
        }
    }
}