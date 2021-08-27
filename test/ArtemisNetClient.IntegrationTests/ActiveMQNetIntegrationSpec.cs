using System;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.MessageIdPolicy;
using ActiveMQ.Artemis.Client.TestUtils;
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

        protected Task<IConnection> CreateConnection()
        {
            var connectionFactory = CreateConnectionFactory();
            var endpoint = GetEndpoint();
            return connectionFactory.CreateAsync(endpoint);
        }

        protected static Endpoint GetEndpoint() => EndpointUtil.GetEndpoint();

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
            return new XUnitLoggerFactory(_output);
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