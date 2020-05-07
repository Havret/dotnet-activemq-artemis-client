using System;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Net.Exceptions;
using ActiveMQ.Net.Transactions;
using Amqp;
using Amqp.Framing;
using Microsoft.Extensions.Logging;

namespace ActiveMQ.Net.Builders
{
    internal class ProducerBuilder
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly TransactionsManager _transactionsManager;
        private readonly Session _session;
        private readonly TaskCompletionSource<bool> _tcs;

        public ProducerBuilder(ILoggerFactory loggerFactory, TransactionsManager transactionsManager, Session session)
        {
            _loggerFactory = loggerFactory;
            _transactionsManager = transactionsManager;
            _session = session;
            _tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public async Task<IProducer> CreateAsync(ProducerConfiguration configuration, CancellationToken cancellationToken)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            if (string.IsNullOrWhiteSpace(configuration.Address)) throw new ArgumentNullException(nameof(configuration.Address), "The address cannot be empty.");

            cancellationToken.ThrowIfCancellationRequested();
            cancellationToken.Register(() => _tcs.TrySetCanceled());

            var routingCapabilities = configuration.RoutingType.GetRoutingCapabilities();
            var target = new Target
            {
                Address = configuration.Address,
                Capabilities = routingCapabilities
            };
            var senderLink = new SenderLink(_session, Guid.NewGuid().ToString(), target, OnAttached);
            senderLink.AddClosedCallback(OnClosed);
            await _tcs.Task.ConfigureAwait(false);
            var producer = new Producer(_loggerFactory, senderLink, _transactionsManager, configuration);
            senderLink.Closed -= OnClosed;
            return producer;
        }
        
        private void OnAttached(ILink link, Attach attach)
        {
            if (attach.Source != null)
            {
                _tcs.TrySetResult(true);
            }
        }
        
        private void OnClosed(IAmqpObject sender, Error error)
        {
            if (error != null)
            {
                _tcs.TrySetException(CreateProducerException.FromError(error));
            }
        }
    }
}