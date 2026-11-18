using System;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Exceptions;
using ActiveMQ.Artemis.Client.Transactions;
using Amqp;
using Microsoft.Extensions.Logging;

namespace ActiveMQ.Artemis.Client.AutoRecovering
{
    internal class AutoRecoveringAnonymousProducer : AutoRecoveringProducerBase<IAnonymousProducer>, IAnonymousProducer
    {
        private readonly AnonymousProducerConfiguration _configuration;

        public AutoRecoveringAnonymousProducer(ILoggerFactory loggerFactory, AnonymousProducerConfiguration configuration) : base(loggerFactory)
        {
            _configuration = configuration;
        }
        
        public async Task SendAsync(string address, RoutingType? routingType, Message message, Transaction transaction, CancellationToken cancellationToken = default)
        {
            while (true)
            {
                CheckState();
                var producer = Producer;

                try
                {
                    await producer.SendAsync(address, routingType, message, transaction, cancellationToken).ConfigureAwait(false);
                    return;
                }
                catch (ProducerClosedException e) when (e.ErrorCode is ErrorCode.UnauthorizedAccess)
                {
                    if (await TryTerminateAsync(producer, e).ConfigureAwait(false))
                    {
                        // Producer cannot be recovered when broker denies authorization.
                        throw;
                    }

                    // A late failure of a producer that has already been replaced, wait until the replacement is resumed
                    await WaitAsync(cancellationToken).ConfigureAwait(false);
                    Log.RetryingSendAsync(Logger, address);
                }
                catch (ProducerClosedException)
                {
                    HandleProducerClosed(producer);
                    await WaitAsync(cancellationToken).ConfigureAwait(false);
                    Log.RetryingSendAsync(Logger, address);
                }
                catch (ObjectDisposedException) when (!ReferenceEquals(producer, Producer))
                {
                    // The producer was replaced and disposed by the recovery in the meantime, wait until the replacement is resumed
                    await WaitAsync(cancellationToken).ConfigureAwait(false);
                    Log.RetryingSendAsync(Logger, address);
                }
            }
        }

        public void Send(string address, RoutingType? routingType, Message message, CancellationToken cancellationToken)
        {
            while (true)
            {
                CheckState();
                var producer = Producer;

                try
                {
                    producer.Send(address, routingType, message, cancellationToken);
                    return;
                }
                catch (ProducerClosedException e) when (e.ErrorCode is ErrorCode.UnauthorizedAccess)
                {
                    if (TryTerminateAsync(producer, e).GetAwaiter().GetResult())
                    {
                        // Producer cannot be recovered when broker denies authorization.
                        throw;
                    }

                    // A late failure of a producer that has already been replaced, wait until the replacement is resumed
                    Wait(cancellationToken);
                    Log.RetryingSendAsync(Logger, address);
                }
                catch (ProducerClosedException)
                {
                    HandleProducerClosed(producer);
                    Wait(cancellationToken);
                    Log.RetryingSendAsync(Logger, address);
                }
                catch (ObjectDisposedException) when (!ReferenceEquals(producer, Producer))
                {
                    // The producer was replaced and disposed by the recovery in the meantime, wait until the replacement is resumed
                    Wait(cancellationToken);
                    Log.RetryingSendAsync(Logger, address);
                }
            }
        }

        protected override async Task<IAnonymousProducer> CreateUnderlyingProducer(IConnection connection, CancellationToken cancellationToken)
        {
            return await connection.CreateAnonymousProducerAsync(_configuration, cancellationToken).ConfigureAwait(false);
        }
    }
}