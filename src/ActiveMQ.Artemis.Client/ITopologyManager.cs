using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ActiveMQ.Artemis.Client
{
    public interface ITopologyManager : IAsyncDisposable
    {
        Task<IReadOnlyList<string>> GetAddressNames(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<string>> GetQueueNames(CancellationToken cancellationToken = default);
        Task CreateAddress(string name, RoutingType routingType, CancellationToken cancellationToken = default);
        Task CreateAddress(string name, IEnumerable<RoutingType> routingTypes, CancellationToken cancellationToken = default);
        Task CreateQueue(QueueConfiguration configuration, CancellationToken cancellationToken = default);
    }
}