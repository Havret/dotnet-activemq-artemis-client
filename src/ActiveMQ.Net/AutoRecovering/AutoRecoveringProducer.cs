using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Net.Exceptions;
using ActiveMQ.Net.Transactions;
using Microsoft.Extensions.Logging;

namespace ActiveMQ.Net.AutoRecovering
{
    internal class AutoRecoveringProducer : AutoRecoveringProducerBase, IProducer
    {
        private readonly ProducerConfiguration _configuration;
        private IProducer _producer;

        public AutoRecoveringProducer(ILoggerFactory loggerFactory, ProducerConfiguration configuration) : base(loggerFactory)
        {
            _configuration = configuration;
        }

        public async Task SendAsync(Message message, Transaction transaction, CancellationToken cancellationToken = default)
        {
            while (true)
            {
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

        public void Send(Message message)
        {
            while (true)
            {
                try
                {
                    _producer.Send(message);
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
            _producer = await connection.CreateProducerAsync(_configuration, cancellationToken).ConfigureAwait(false);
        }

        protected override ValueTask DisposeUnderlyingProducer()
        {
            return _producer.DisposeAsync();
        }
    }
}