using System.Threading.Tasks;
using Amqp;
using Microsoft.Extensions.Logging;

namespace ActiveMQ.Net.Builders
{
    internal class ConnectionBuilder
    {
        private readonly ILoggerFactory _loggerFactory;

        public ConnectionBuilder(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
        }
        
        public async Task<IConnection> CreateAsync(string address)
        {
            var connectionFactory = new Amqp.ConnectionFactory();
            var connection = await connectionFactory.CreateAsync(new Address(address)).ConfigureAwait(false);
            var session = new Session(connection);
            return new Connection(_loggerFactory, connection, session);
        }
    }
}