using System.Threading.Tasks;
using Amqp;

namespace ActiveMQ.Net
{
    public class ConnectionFactory
    {
        public async Task<IConnection> CreateAsync(string address)
        {
            var connectionFactory = new Amqp.ConnectionFactory();
            var connection = await connectionFactory.CreateAsync(new Address(address)).ConfigureAwait(false);
            var session = new Session(connection);
            return new Connection(connection, session);
        }
    }
}