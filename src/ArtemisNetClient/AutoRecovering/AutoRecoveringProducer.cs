using System;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Exceptions;
using ActiveMQ.Artemis.Client.Transactions;
using Amqp;
using Microsoft.Extensions.Logging;

namespace ActiveMQ.Artemis.Client.AutoRecovering
{
    internal class AutoRecoveringProducer : AutoRecoveringProducerBase<IProducer>, IProducer
    {
        private readonly ProducerConfiguration _configuration;

        public AutoRecoveringProducer(ILoggerFactory loggerFactory, ProducerConfiguration configuration) : base(loggerFactory)
        {
            _configuration = configuration;
        }

        public async Task SendAsync(Message message, Transaction transaction, CancellationToken cancellationToken = default)
        {
            while (true)
            {
                CheckState();
                var producer = Producer;

                try
                {
                    await producer.SendAsync(message, transaction, cancellationToken).ConfigureAwait(false);
                    return;
                }
                catch (ProducerClosedException e) when (IsTerminalProducerException(e))
                {
                    if (await TryTerminateAsync(producer, e).ConfigureAwait(false))
                    {
                        // Producer cannot be recovered for terminal broker-side close reasons.
                        throw;
                    }

                    // A late failure of a producer that has already been replaced, wait until the replacement is resumed
                    await WaitAsync(cancellationToken).ConfigureAwait(false);
                    Log.RetryingSendAsync(Logger, _configuration.Address);
                }
                catch (ProducerClosedException)
                {
                    HandleProducerClosed(producer);
                    await WaitAsync(cancellationToken).ConfigureAwait(false);
                    Log.RetryingSendAsync(Logger, _configuration.Address);
                }
                catch (ObjectDisposedException) when (!ReferenceEquals(producer, Producer))
                {
                    // The producer was replaced and disposed by the recovery in the meantime, wait until the replacement is resumed
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
                var producer = Producer;

                try
                {
                    producer.Send(message, cancellationToken);
                    return;
                }
                catch (ProducerClosedException e) when (IsTerminalProducerException(e))
                {
                    if (TryTerminateAsync(producer, e).GetAwaiter().GetResult())
                    {
                        // Producer cannot be recovered for terminal broker-side close reasons.
                        throw;
                    }

                    // A late failure of a producer that has already been replaced, wait until the replacement is resumed
                    Wait(cancellationToken);
                    Log.RetryingSendAsync(Logger, _configuration.Address);
                }
                catch (ProducerClosedException)
                {
                    HandleProducerClosed(producer);
                    Wait(cancellationToken);
                    Log.RetryingSendAsync(Logger, _configuration.Address);
                }
                catch (ObjectDisposedException) when (!ReferenceEquals(producer, Producer))
                {
                    // The producer was replaced and disposed by the recovery in the meantime, wait until the replacement is resumed
                    Wait(cancellationToken);
                    Log.RetryingSendAsync(Logger, _configuration.Address);
                }
            }
        }

        private static bool IsTerminalProducerException(ProducerClosedException exception)
        {
            return exception.ErrorCode is ErrorCode.UnauthorizedAccess or ErrorCode.NotFound;
        }

        protected override async Task<IProducer> CreateUnderlyingProducer(IConnection connection, CancellationToken cancellationToken)
        {
            return await connection.CreateProducerAsync(_configuration, cancellationToken).ConfigureAwait(false);
        }
    }
}