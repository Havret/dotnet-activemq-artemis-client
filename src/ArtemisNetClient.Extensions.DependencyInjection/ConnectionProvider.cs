using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Extensions.DependencyInjection.InternalUtils;
using Microsoft.Extensions.DependencyInjection;

namespace ActiveMQ.Artemis.Client.Extensions.DependencyInjection
{
    internal class ConnectionProvider
    {
        private readonly IServiceProvider _serviceProvider;

        public ConnectionProvider(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async ValueTask<IConnection> GetConnection(string name, CancellationToken cancellationToken)
        {
            var namedConnection = _serviceProvider.GetServices<NamedConnection>().First(x => x.Name == name);
            return await namedConnection.Connection.GetValueAsync(cancellationToken);
        }

        public AsyncValueLazy<IConnection> GetConnection(string name)
        {
            var namedConnection = _serviceProvider.GetServices<NamedConnection>().First(x => x.Name == name);
            return namedConnection.Connection;
        }
    }
}