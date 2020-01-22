using System.Threading.Tasks;
using ActiveMQ.Net.AutoRecovering;
using ActiveMQ.Net.Builders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ActiveMQ.Net
{
    public class ConnectionFactory
    {
        public async Task<IConnection> CreateAsync(string address)
        {
            if (AutomaticRecoveryEnabled)
            {
                var autoRecoveringConnection = new AutoRecoveringConnection(LoggerFactory, address);
                await autoRecoveringConnection.InitAsync().ConfigureAwait(false);
                return autoRecoveringConnection;
            }
            else
            {
                var connectionBuilder = new ConnectionBuilder(LoggerFactory);
                return await connectionBuilder.CreateAsync(address).ConfigureAwait(false);
            }
        }

        public ILoggerFactory LoggerFactory { get; set; } = new NullLoggerFactory();
        public bool AutomaticRecoveryEnabled { get; set; } = true;
    }
}