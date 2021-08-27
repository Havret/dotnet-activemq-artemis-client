using System;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Extensions.DependencyInjection.InternalUtils;

namespace ActiveMQ.Artemis.Client.Extensions.DependencyInjection
{
    internal class NamedConnection
    {
        public NamedConnection(string name, Func<CancellationToken, Task<IConnection>> factory)
        {
            Name = name;
            Connection = new AsyncValueLazy<IConnection>(factory);
        }

        public string Name { get; }

        public AsyncValueLazy<IConnection> Connection { get; }
    }
}