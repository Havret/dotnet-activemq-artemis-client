using System;
using System.Collections.Generic;
using System.Linq;
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
        private static readonly Symbol _sharedCapability = new Symbol("shared");
        private static readonly Symbol _globalCapability = new Symbol("global");

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
            cancellationToken.Register(() => _tcs.TrySetCanceled());

            var source = new Source
            {
                Address = GetAddress(configuration),
                Capabilities = GetCapabilities(configuration),
                FilterSet = GetFilterSet(configuration.FilterExpression, configuration.NoLocalFilter),
                Durable = (uint) GetTerminusDurability(configuration),
            };

            var receiverLink = new ReceiverLink(_session, GetLinkName(configuration), source, OnAttached);
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
            if (configuration.RoutingType == RoutingType.Anycast)
            {
                if (configuration.NoLocalFilter)
                    throw new ArgumentException($"{nameof(ConsumerConfiguration.NoLocalFilter)} cannot be used with {RoutingType.Anycast.ToString()} routing type.",
                        nameof(configuration.NoLocalFilter));
                if (!string.IsNullOrEmpty(configuration.Queue))
                    throw new ArgumentException($"Queue name cannot be explicitly set for {nameof(RoutingType.Anycast)} {nameof(RoutingType)}. " +
                                                $"If you want to attach to pre-configured queue do not set any {nameof(RoutingType)}.", nameof(configuration.Queue));
            }
            if (configuration.RoutingType == RoutingType.Multicast)
            {
                if (!configuration.Durable && !configuration.Shared && !string.IsNullOrWhiteSpace(configuration.Queue))
                {
                    throw new ArgumentException("Cannot explicitly set queue name for unshared, non-durable subscription.");
                }
            }
            if (!configuration.RoutingType.HasValue) 
            {
                if (configuration.Durable)
                {
                    throw new ArgumentException("Cannot explicitly select durable subscription on attempt to attach to pre-configured queue. " +
                                                "Option would be overriden by broker configuration.", nameof(configuration.Durable));
                }
                if (configuration.Shared)
                {
                    throw new ArgumentException("Cannot explicitly select shared subscription on attempt to attach to pre-configured queue. " +
                                                "Option would be overriden by broker configuration.", nameof(configuration.Shared));
                }
                if (string.IsNullOrWhiteSpace(configuration.Queue))
                {
                    throw new ArgumentNullException(nameof(configuration.Queue), "Cannot attach to pre-configured queue when queue name not provided.");
                }
            }
        }

        private TerminusDurability GetTerminusDurability(ConsumerConfiguration configuration)
        {
            if (configuration.Durable)
                return TerminusDurability.Configuration;
            else
                return TerminusDurability.None;
        }

        private static string GetLinkName(ConsumerConfiguration configuration)
        {
            if (configuration.Shared || !string.IsNullOrEmpty(configuration.Queue))
            {
                return configuration.Queue;
            }

            return Guid.NewGuid().ToString();
        }

        private static Symbol[] GetCapabilities(ConsumerConfiguration configuration)
        {
            var capabilities = new List<Symbol>();

            if (configuration.RoutingType.HasValue)
            {
                capabilities.Add(configuration.RoutingType.Value.GetRoutingCapability());
            }

            if (configuration.Shared)
            {
                capabilities.Add(_sharedCapability);
                capabilities.Add(_globalCapability);
            }

            return capabilities.Any() ? capabilities.ToArray() : null;
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