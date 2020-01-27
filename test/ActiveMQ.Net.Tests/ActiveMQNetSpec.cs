using System;
using System.Collections.Generic;
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

        protected static Endpoint GetUniqueEndpoint()
        {
            return EndpointUtil.GetUniqueEndpoint();
        }

        protected Task<IConnection> CreateConnection(Endpoint endpoint)
        {
            var connectionFactory = CreateConnectionFactory();
            return connectionFactory.CreateAsync(endpoint);
        }

        protected Task<IConnection> CreateConnection(IEnumerable<Endpoint> endpoint)
        {
            var connectionFactory = CreateConnectionFactory();
            return connectionFactory.CreateAsync(endpoint);
        }

        protected ConnectionFactory CreateConnectionFactory()
        {
            return new ConnectionFactory { LoggerFactory = CreateTestLoggerFactory() };
        }

        protected ILoggerFactory CreateTestLoggerFactory()
        {
            return new TestLoggerFactory(_output);
        }

        protected static TestContainerHost CreateOpenedContainerHost(Endpoint endpoint, IHandler handler = null)
        {
            var host = new TestContainerHost(endpoint, handler);
            host.Open();
            return host;
        }
        
        protected static TestContainerHost CreateContainerHost(Endpoint endpoint, IHandler handler = null)
        {
            return new TestContainerHost(endpoint, handler);
        }

        protected static TestContainerHost CreateContainerHostThatWillNeverSendAttachFrameBack(Endpoint endpoint)
        {
            var host = CreateOpenedContainerHost(endpoint);
            var linkProcessor = host.CreateTestLinkProcessor();
            
            // do not complete link attachment, as a result no Attach frame will be sent back to the client
            linkProcessor.SetHandler(context => true);

            return host;
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