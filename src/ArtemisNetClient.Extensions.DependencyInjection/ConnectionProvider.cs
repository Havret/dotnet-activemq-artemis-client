using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Extensions.DependencyInjection.InternalUtils;
using Microsoft.Extensions.DependencyInjection;

namespace ActiveMQ.Artemis.Client.Extensions.DependencyInjection
{
    /// <summary>
    /// Helper class for managing connections. It provides a method for obtaining connections by name.
    /// </summary>
    public class ConnectionProvider
    {
        private readonly IServiceProvider _serviceProvider;

        internal ConnectionProvider(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// A method that can be used to obtain an IConnection object by its name.
        /// </summary>
        /// <param name="name">
        /// A name of the connection. It should match the name passed in
        /// to the <see cref="ActiveMqArtemisClientDependencyInjectionExtensions.AddActiveMq"/> method when
        /// the connection was registered in the <see cref="IServiceCollection"/>.
        /// </param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>
        /// Instance of <see cref="IConnection"/> object that represents a connection
        /// to the ActiveMQ Artemis broker.
        /// </returns>
        public async ValueTask<IConnection> GetConnectionAsync(string name, CancellationToken cancellationToken = default)
        {
            var namedConnection = _serviceProvider.GetServices<NamedConnection>().FirstOrDefault(x => x.Name == name);
            if (namedConnection == null)
            {
                throw new InvalidOperationException($"There is connection registered with name {name}");
            }
            
            return await namedConnection.Connection.GetValueAsync(cancellationToken).ConfigureAwait(false);
        }

        internal AsyncValueLazy<IConnection> GetConnection(string name)
        {
            var namedConnection = _serviceProvider.GetServices<NamedConnection>().First(x => x.Name == name);
            return namedConnection.Connection;
        }
    }
}