﻿using System;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Exceptions;
using ActiveMQ.Artemis.Client.Transactions;
using Amqp;
using Amqp.Framing;
using Amqp.Types;
using Microsoft.Extensions.Logging;

namespace ActiveMQ.Artemis.Client.Builders
{
    internal class ConsumerBuilder
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly TransactionsManager _transactionsManager;
        private readonly Session _session;
        private readonly TaskCompletionSource<bool> _tcs;

        public ConsumerBuilder(ILoggerFactory loggerFactory, TransactionsManager transactionsManager, Session session)
        {
            _loggerFactory = loggerFactory;
            _transactionsManager = transactionsManager;
            _session = session;
            _tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public async Task<IConsumer> CreateAsync(ConsumerConfiguration configuration, CancellationToken cancellationToken)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            if (string.IsNullOrWhiteSpace(configuration.Address)) throw new ArgumentNullException(nameof(configuration.Address), "The address cannot be empty.");
            if (configuration.Credit < 1) throw new ArgumentOutOfRangeException(nameof(configuration.Credit), "Credit should be >= 1.");
            if (configuration.RoutingType == QueueRoutingType.Anycast && configuration.NoLocalFilter)
                throw new ArgumentException($"{nameof(ConsumerConfiguration.NoLocalFilter)} cannot be used with {QueueRoutingType.Anycast.ToString()} routing type.");

            cancellationToken.ThrowIfCancellationRequested();
            cancellationToken.Register(() => _tcs.TrySetCanceled());

            var routingCapability = configuration.RoutingType.GetRoutingCapability();
            var source = new Source
            {
                Address = GetAddress(configuration.Address, configuration.Queue),
                Capabilities = new[] { routingCapability },
                FilterSet = GetFilterSet(configuration.FilterExpression, configuration.NoLocalFilter)
            };

            var receiverLink = new ReceiverLink(_session, Guid.NewGuid().ToString(), source, OnAttached);
            receiverLink.AddClosedCallback(OnClosed);
            await _tcs.Task.ConfigureAwait(false);
            receiverLink.Closed -= OnClosed;
            return new Consumer(_loggerFactory, receiverLink, _transactionsManager, configuration);
        }

        private static string GetAddress(string address, string queue)
        {
            return string.IsNullOrEmpty(queue)
                ? address
                : CreateFullyQualifiedQueueName(address, queue);
        }

        private static Map GetFilterSet(string filterExpression, bool noLocalFilter)
        {
            var filterSet = new Map();
            if (!string.IsNullOrWhiteSpace(filterExpression))
            {
                filterSet.Add(FilterExpression.FilterExpressionName, new FilterExpression(filterExpression));
            }

            if (noLocalFilter)
            {
                filterSet.Add(NoLocalFilter.NoLocalFilterName, NoLocalFilter.Instance);
            }

            return filterSet;
        }

        private static string CreateFullyQualifiedQueueName(string address, string queue)
        {
            return $"{address}::{queue}";
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
                _tcs.TrySetException(CreateConsumerException.FromError(error));
            }
        }
    }
}