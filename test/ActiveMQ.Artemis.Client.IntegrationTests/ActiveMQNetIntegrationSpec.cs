using System;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.MessageIdPolicy;
using ActiveMQ.Artemis.Client.TestUtils.Logging;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace ActiveMQ.Artemis.Client.IntegrationTests
{
    public abstract class ActiveMQNetIntegrationSpec
    {
        private readonly ITestOutputHelper _output;

        protected ActiveMQNetIntegrationSpec(ITestOutputHelper output)
        {
            _output = output;
        }
        
        protected Task<IConnection> CreateConnectionWithoutAutomaticRecovery()
        {
            var connectionFactory = CreateConnectionFactory();
            var endpoint = GetEndpoint();
            connectionFactory.AutomaticRecoveryEnabled = false;
            return connectionFactory.CreateAsync(endpoint);
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
            return new ConnectionFactory
            {
                LoggerFactory = CreateTestLoggerFactory(),
                MessageIdPolicyFactory = MessageIdPolicyFactory.GuidMessageIdPolicy
            };
        }

        private ILoggerFactory CreateTestLoggerFactory()
        {
            return new TestLoggerFactory(_output);
        }

        protected static CancellationToken CancellationToken => new CancellationTokenSource(Timeout).Token;

        private static TimeSpan Timeout
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
    }
}