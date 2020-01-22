using System;
using System.Threading.Tasks;
using ActiveMQ.Net.Tests.Logging;
using ActiveMQ.Net.Tests.Utils;
using Amqp.Handler;
using Microsoft.Extensions.Logging;
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
            var connectionFactory = new ConnectionFactory { LoggerFactory = CreateTestLoggerFactory() };
            return connectionFactory.CreateAsync(address);
        }

        protected ILoggerFactory CreateTestLoggerFactory()
        {
            return new TestLoggerFactory(_output);
        }

        protected static TestContainerHost CreateOpenedContainerHost(string address, IHandler handler = null)
        {
            var host = new TestContainerHost(address, handler);
            host.Open();
            return host;
        }
        
        protected static TestContainerHost CreateContainerHost(string address, IHandler handler = null)
        {
            return new TestContainerHost(address, handler);
        }

        protected static TimeSpan Timeout
        {
            get
            {
#if DEBUG
                return TimeSpan.FromMinutes(1);
#else
                return TimeSpan.FromSeconds(10);
#endif
            }
        }

        protected static TimeSpan ShortTimeout = TimeSpan.FromMilliseconds(100);
    }
}