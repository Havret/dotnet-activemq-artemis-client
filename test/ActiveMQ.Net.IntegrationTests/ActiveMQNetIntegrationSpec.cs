using System;
using System.Threading.Tasks;
using ActiveMQ.Net.TestUtils.Logging;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace ActiveMQ.Net.IntegrationTests
{
    public abstract class ActiveMQNetIntegrationSpec
    {
        private readonly ITestOutputHelper _output;

        protected ActiveMQNetIntegrationSpec(ITestOutputHelper output)
        {
            _output = output;
        }
        
        protected Task<IConnection> CreateConnection()
        {
            var connectionFactory = CreateConnectionFactory();
            var endpoint = GetEndpoint();
            return connectionFactory.CreateAsync(endpoint);
        }

        private static Endpoint GetEndpoint()
        {
            string userName = Environment.GetEnvironmentVariable("ARTEMIS_USERNAME") ?? "guest";
            string password = Environment.GetEnvironmentVariable("ARTEMIS_PASSWORD") ?? "guest";
            string host = Environment.GetEnvironmentVariable("ARTEMIS_HOST") ?? "localhost";
            int port = int.Parse(Environment.GetEnvironmentVariable("ARTEMIS_PORT") ?? "5672");
            return Endpoint.Create(host, port, userName, password);
        }

        private ConnectionFactory CreateConnectionFactory()
        {
            return new ConnectionFactory { LoggerFactory = CreateTestLoggerFactory() };
        }

        private ILoggerFactory CreateTestLoggerFactory()
        {
            return new TestLoggerFactory(_output);
        }
    }
}