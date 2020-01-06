using System.Threading.Tasks;
using ActiveMQ.Net.Tests.Logging;
using ActiveMQ.Net.Tests.Utils;
using Amqp.Handler;
using Xunit.Abstractions;

namespace ActiveMQ.Net.Tests
{
    public class ActiveMQNetSpec
    {
        private readonly ITestOutputHelper _output;

        protected ActiveMQNetSpec(ITestOutputHelper output)
        {
            _output = output;
        }

        protected static string GetUniqueAddress()
        {
            return AddressUtil.GetUniqueAddress();
        }

        protected Task<IConnection> CreateConnection(string address)
        {

            var connectionFactory = new ConnectionFactory { LoggerFactory = new TestLoggerFactory(_output) };
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