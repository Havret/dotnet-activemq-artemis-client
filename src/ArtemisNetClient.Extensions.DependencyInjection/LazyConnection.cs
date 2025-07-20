using System;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Extensions.DependencyInjection.InternalUtils;

namespace ActiveMQ.Artemis.Client.Extensions.DependencyInjection;

/// <summary>
/// Represents a lazily initialized connection to an ActiveMQ Artemis broker.
/// </summary>
internal class LazyConnection(Func<CancellationToken, Task<IConnection>> factory)
{
    /// <summary>
    /// Gets the lazily initialized connection.
    /// </summary>
    public AsyncValueLazy<IConnection> Connection { get; } = new(factory);
}