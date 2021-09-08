﻿using System;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Exceptions;
using ActiveMQ.Artemis.Client.MessageIdPolicy;
using ActiveMQ.Artemis.Client.Transactions;
using Amqp;
using Amqp.Framing;
using Amqp.Types;
using Microsoft.Extensions.Logging;

namespace ActiveMQ.Artemis.Client.Builders
{
    internal class ProducerBuilder
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly TransactionsManager _transactionsManager;
        private readonly Session _session;
        private readonly Func<IMessageIdPolicy> _messageIdPolicyFactory;
        private readonly TaskCompletionSource<bool> _tcs;

        public ProducerBuilder(ILoggerFactory loggerFactory, TransactionsManager transactionsManager, Session session, Func<IMessageIdPolicy> messageIdPolicyFactory)
        {
            _loggerFactory = loggerFactory;
            _transactionsManager = transactionsManager;
            _session = session;
            _messageIdPolicyFactory = messageIdPolicyFactory;
            _tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public async Task<IProducer> CreateAsync(ProducerConfiguration configuration, CancellationToken cancellationToken)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            if (string.IsNullOrWhiteSpace(configuration.Address)) throw new ArgumentNullException(nameof(configuration.Address), "The address cannot be empty.");

            cancellationToken.ThrowIfCancellationRequested();
            using var _ = cancellationToken.Register(() => _tcs.TrySetCanceled());

            var target = new Target
            {
                Address = configuration.Address,
                Capabilities = GetCapabilities(configuration)
            };
            var senderLink = new SenderLink(_session, Guid.NewGuid().ToString(), target, OnAttached);
            senderLink.AddClosedCallback(OnClosed);
            await _tcs.Task.ConfigureAwait(false);
            configuration.MessageIdPolicy ??= _messageIdPolicyFactory();
            var producer = new Producer(_loggerFactory, senderLink, _transactionsManager, configuration);
            senderLink.Closed -= OnClosed;
            return producer;
        }

        private static Symbol[] GetCapabilities(ProducerConfiguration configuration)
        {
            return configuration.RoutingType.HasValue
                ? new[] { configuration.RoutingType.Value.GetRoutingCapability() }
                : null;
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
                _tcs.TrySetException(new CreateProducerException(error.Description, error.Condition));
            }
        }
    }
}