using System;
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
            CheckConfiguration(configuration);

            cancellationToken.ThrowIfCancellationRequested();
            using var _ = cancellationToken.Register(() => _tcs.TrySetCanceled());

            var source = new Source
            {
                Address = GetAddress(configuration),
                Capabilities = GetCapabilities(configuration),
                FilterSet = GetFilterSet(configuration.FilterExpression, configuration.NoLocalFilter),
            };

            var receiverLink = new ReceiverLink(_session, Guid.NewGuid().ToString(), source, OnAttached);
            receiverLink.AddClosedCallback(OnClosed);
            await _tcs.Task.ConfigureAwait(false);
            receiverLink.Closed -= OnClosed;
            return new Consumer(_loggerFactory, receiverLink, _transactionsManager, configuration);
        }

        private static void CheckConfiguration(ConsumerConfiguration configuration)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            if (string.IsNullOrWhiteSpace(configuration.Address)) throw new ArgumentNullException(nameof(configuration.Address), "The address cannot be empty.");
            if (configuration.Credit < 1) throw new ArgumentOutOfRangeException(nameof(configuration.Credit), "Credit should be >= 1.");
            if (configuration.RoutingType == RoutingType.Anycast && configuration.NoLocalFilter)
            {
                throw new ArgumentException($"{nameof(ConsumerConfiguration.NoLocalFilter)} cannot be used with {RoutingType.Anycast.ToString()} routing type.",
                    nameof(configuration.NoLocalFilter));
            }
            if (configuration.RoutingType.HasValue && !string.IsNullOrEmpty(configuration.Queue))
            {
                throw new ArgumentException($"Queue name cannot be explicitly set when {nameof(RoutingType)} provided. " +
                                            $"If you want to attach to queue by name, do not set any {nameof(RoutingType)}.", nameof(configuration.Queue));
            }
            if (!configuration.RoutingType.HasValue && string.IsNullOrWhiteSpace(configuration.Queue))
            {
                throw new ArgumentNullException(nameof(configuration.Queue), "Cannot attach to queue when queue name not provided.");
            }
        }

        private static Symbol[] GetCapabilities(ConsumerConfiguration configuration)
        {
            return configuration.RoutingType.HasValue ? new[] { configuration.RoutingType.Value.GetRoutingCapability() } : null;
        }

        private static string GetAddress(ConsumerConfiguration configuration)
        {
            if (configuration.RoutingType.HasValue)
            {
                return configuration.Address;
            }
            else
            {
                return CreateFullyQualifiedQueueName(configuration.Address, configuration.Queue);
            }
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
                _tcs.TrySetException(new CreateConsumerException(error.Description, error.Condition));
            }
        }
    }
}