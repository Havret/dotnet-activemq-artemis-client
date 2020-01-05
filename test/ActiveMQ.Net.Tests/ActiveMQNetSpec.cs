using System.Threading.Tasks;
using ActiveMQ.Net.Tests.Utils;
using Amqp.Handler;

namespace ActiveMQ.Net.Tests
{
    public class ActiveMQNetSpec
    {
        protected static string GetUniqueAddress() => AddressUtil.GetUniqueAddress();

        protected static Task<IConnection> CreateConnection(string address)
        {
            var connectionFactory = new ConnectionFactory();
            return connectionFactory.CreateAsync(address);
        }

        protected static TestContainerHost CreateOpenedContainerHost(string address, IHandler handler = null)
        {
            var host = new TestContainerHost(address, handler);
            host.Open();
            return host;
        }
    }
}