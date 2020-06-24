using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ActiveMQ.Artemis.Client
{
    public interface ITopologyManager : IAsyncDisposable
    {
        Task<IReadOnlyList<string>> GetAddressNamesAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<string>> GetQueueNamesAsync(CancellationToken cancellationToken = default);
        Task CreateAddressAsync(string name, RoutingType routingType, CancellationToken cancellationToken = default);
        Task CreateAddressAsync(string name, IEnumerable<RoutingType> routingTypes, CancellationToken cancellationToken = default);
        Task DeclareAddressAsync(string name, RoutingType routingType, CancellationToken cancellationToken = default);
        Task DeclareAddressAsync(string name, IEnumerable<RoutingType> routingTypes, CancellationToken cancellationToken = default);
        Task CreateQueueAsync(QueueConfiguration configuration, CancellationToken cancellationToken = default);
        Task DeleteQueue(string queueName, bool removeConsumers = false, bool autoDeleteAddress = false, CancellationToken cancellationToken = default);
    }
}