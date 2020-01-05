using System.Threading.Tasks;
using ActiveMQ.Net.AutoRecovering;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ActiveMQ.Net
{
    public class ConnectionFactory
    {
        public async Task<IConnection> CreateAsync(string address)
        {
            var autoRecoveringConnection = new AutoRecoveringConnection(LoggerFactory, address);
            await autoRecoveringConnection.InitAsync().ConfigureAwait(false);
            return autoRecoveringConnection;
        }

        public ILoggerFactory LoggerFactory { get; set; } = new NullLoggerFactory();
    }
}