using System;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Exceptions;
using ActiveMQ.Artemis.Client.Transactions;
using Microsoft.Extensions.Logging;

namespace ActiveMQ.Artemis.Client.AutoRecovering
{
    internal class AutoRecoveringProducer : AutoRecoveringProducerBase, IProducer
    {
        private readonly ProducerConfiguration _configuration;
        private volatile IProducer _producer;

        public AutoRecoveringProducer(ILoggerFactory loggerFactory, ProducerConfiguration configuration) : base(loggerFactory)
        {
            _configuration = configuration;
        }

        public async Task SendAsync(Message message, Transaction transaction, CancellationToken cancellationToken = default)
        {
            while (true)
            {
                CheckState();

                try
                {
                    await _producer.SendAsync(message, transaction, cancellationToken).ConfigureAwait(false);
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

        public void Send(Message message, CancellationToken cancellationToken)
        {
            while (true)
            {
                CheckState();

                try
                {
                    _producer.Send(message, cancellationToken);
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
            _producer = await connection.CreateProducerAsync(_configuration, cancellationToken).ConfigureAwait(false);
        }
    }
}