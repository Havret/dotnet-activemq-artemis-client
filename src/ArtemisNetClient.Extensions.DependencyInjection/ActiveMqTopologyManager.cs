using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Extensions.DependencyInjection.InternalUtils;

namespace ActiveMQ.Artemis.Client.Extensions.DependencyInjection
{
    internal class ActiveMqTopologyManager
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly AsyncValueLazy<IConnection> _lazyConnection;
        private readonly IReadOnlyList<QueueConfiguration> _queueConfigurations;
        private readonly IReadOnlyDictionary<string, HashSet<RoutingType>> _addressConfigurations;
        private readonly List<Func<IServiceProvider, ITopologyManager, Task>> _configureTopologyActions;

        public ActiveMqTopologyManager(IServiceProvider serviceProvider,
            AsyncValueLazy<IConnection> lazyConnection,
            IReadOnlyList<QueueConfiguration> queueConfigurations,
            IReadOnlyDictionary<string, HashSet<RoutingType>> addressConfigurations,
            List<Func<IServiceProvider, ITopologyManager, Task>> configureTopologyActions)
        {
            _serviceProvider = serviceProvider;
            _lazyConnection = lazyConnection;
            _queueConfigurations = queueConfigurations;
            _addressConfigurations = addressConfigurations;
            _configureTopologyActions = configureTopologyActions;
        }

        public async Task CreateTopologyAsync(CancellationToken cancellationToken)
        {
            if (_queueConfigurations.Count == 0 && _addressConfigurations.Count == 0 && _configureTopologyActions.Count == 0)
            {
                return;
            }

            var connection = await _lazyConnection.GetValueAsync(cancellationToken).ConfigureAwait(false);
            var topologyManager = await connection.CreateTopologyManagerAsync(cancellationToken).ConfigureAwait(false);
            await using var _ = topologyManager.ConfigureAwait(false);

            foreach (var configureTopologyAction in _configureTopologyActions)
            {
                await configureTopologyAction(_serviceProvider, topologyManager).ConfigureAwait(false);
            }

            foreach (var addressConfiguration in _addressConfigurations)
            {
                await topologyManager.DeclareAddressAsync(addressConfiguration.Key, addressConfiguration.Value, cancellationToken).ConfigureAwait(false);
            }
            
            var queues = await topologyManager.GetQueueNamesAsync(cancellationToken).ConfigureAwait(false);
            foreach (var queueConfiguration in _queueConfigurations.Where(x => !queues.Contains(x.Name)))
            {
                await topologyManager.DeclareQueueAsync(queueConfiguration, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}