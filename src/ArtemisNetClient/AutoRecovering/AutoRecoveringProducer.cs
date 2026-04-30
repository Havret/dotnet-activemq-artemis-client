using System;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Exceptions;
using ActiveMQ.Artemis.Client.Transactions;
using Amqp;
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
                var producer = _producer;

                try
                {
                    await producer.SendAsync(message, transaction, cancellationToken).ConfigureAwait(false);
                    return;
                }
                catch (ProducerClosedException e) when (IsTerminalProducerException(e))
                {
                    await TerminateAsync(e).ConfigureAwait(false);

                    // Producer cannot be recovered for terminal broker-side close reasons.
                    throw;
                }
                catch (ProducerClosedException)
                {
                    HandleProducerClosed();
                    await WaitAsync(cancellationToken).ConfigureAwait(false);
                    Log.RetryingSendAsync(Logger, _configuration.Address);
                }
                catch (ObjectDisposedException) when (!ReferenceEquals(producer, _producer))
                {
                    HandleProducerClosed();
                    await WaitAsync(cancellationToken).ConfigureAwait(false);
                    Log.RetryingSendAsync(Logger, _configuration.Address);
                }
            }
        }

        public void Send(Message message, CancellationToken cancellationToken)
        {
            while (true)
            {
                CheckState();
                var producer = _producer;

                try
                {
                    producer.Send(message, cancellationToken);
                    return;
                }
                catch (ProducerClosedException e) when (IsTerminalProducerException(e))
                {
                    TerminateAsync(e).GetAwaiter().GetResult();

                    // Producer cannot be recovered for terminal broker-side close reasons.
                    throw;
                }
                catch (ProducerClosedException)
                {
                    HandleProducerClosed();
                    Wait(cancellationToken);
                    Log.RetryingSendAsync(Logger, _configuration.Address);
                }
                catch (ObjectDisposedException) when (!ReferenceEquals(producer, _producer))
                {
                    HandleProducerClosed();
                    Wait(cancellationToken);
                    Log.RetryingSendAsync(Logger, _configuration.Address);
                }
            }
        }

        private static bool IsTerminalProducerException(ProducerClosedException exception)
        {
            return exception.ErrorCode is ErrorCode.UnauthorizedAccess or ErrorCode.NotFound;
        }

        protected override IAsyncDisposable UnderlyingResource => _producer;

        protected override async Task RecoverUnderlyingProducer(IConnection connection, CancellationToken cancellationToken)
        {
            _producer = await connection.CreateProducerAsync(_configuration, cancellationToken).ConfigureAwait(false);
        }
    }
}