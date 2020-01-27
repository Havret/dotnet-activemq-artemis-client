using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ActiveMQ.Net.AutoRecovering;
using ActiveMQ.Net.Builders;
using ActiveMQ.Net.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ActiveMQ.Net
{
    public class ConnectionFactory
    {
        public Task<IConnection> CreateAsync(Endpoint endpoint)
        {
            return CreateAsync(new[] { endpoint });
        }

        public async Task<IConnection> CreateAsync(IEnumerable<Endpoint> endpoints)
        {
            if (!endpoints.Any())
            {
                throw new CreateConnectionException("No endpoints provided.");
            }
            
            if (AutomaticRecoveryEnabled)
            {
                var autoRecoveringConnection = new AutoRecoveringConnection(LoggerFactory, endpoints.First());
                await autoRecoveringConnection.InitAsync().ConfigureAwait(false);
                return autoRecoveringConnection;
            }
            else
            {
                var connectionBuilder = new ConnectionBuilder(LoggerFactory);
                return await connectionBuilder.CreateAsync(endpoints.First()).ConfigureAwait(false);
            }
        }

        public ILoggerFactory LoggerFactory { get; set; } = new NullLoggerFactory();
        public bool AutomaticRecoveryEnabled { get; set; } = true;
    }
}