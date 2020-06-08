using System;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Exceptions;
using ActiveMQ.Artemis.Client.Transactions;
using Microsoft.Extensions.Logging;

namespace ActiveMQ.Artemis.Client.AutoRecovering
{
    internal class AutoRecoveringAnonymousProducer : AutoRecoveringProducerBase, IAnonymousProducer
    {
        private readonly AnonymousProducerConfiguration _configuration;
        private IAnonymousProducer _producer;

        public AutoRecoveringAnonymousProducer(ILoggerFactory loggerFactory, AnonymousProducerConfiguration configuration) : base(loggerFactory)
        {
            _configuration = configuration;
        }
        
        public async Task SendAsync(string address, RoutingType? routingType, Message message, Transaction transaction, CancellationToken cancellationToken = default)
        {
            while (true)
            {
                CheckState();
                
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

        public void Send(string address, RoutingType? routingType, Message message, CancellationToken cancellationToken)
        {
            while (true)
            {
                CheckState();
                
                try
                {
                    _producer.Send(address, routingType, message, cancellationToken);
                    return;
                }
                catch (ProducerClosedException)
                {
                    HandleProducerClosed();
                    Wait(cancellationToken);
                    Log.RetryingSendAsync(Logger);
                }
            }
        }

        protected override IAsyncDisposable UnderlyingResource => _producer;

        protected override async Task RecoverUnderlyingProducer(IConnection connection, CancellationToken cancellationToken)
        {
            _producer = await connection.CreateAnonymousProducerAsync(_configuration, cancellationToken).ConfigureAwait(false);
        }
    }
}