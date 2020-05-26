using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.MessageIdPolicy;
using ActiveMQ.Artemis.Client.TestUtils.Logging;
using ActiveMQ.Artemis.Client.UnitTests.Utils;
using Amqp.Handler;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace ActiveMQ.Artemis.Client.UnitTests
{
    public abstract class ActiveMQNetSpec
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
        
        protected Task<IConnection> CreateConnectionWithoutAutomaticRecovery(Endpoint endpoint)
        {
            var connectionFactory = CreateConnectionFactory();
            connectionFactory.AutomaticRecoveryEnabled = false;
            return connectionFactory.CreateAsync(endpoint);
        }

        protected Task<IConnection> CreateConnection(IEnumerable<Endpoint> endpoint)
        {
            var connectionFactory = CreateConnectionFactory();
            return connectionFactory.CreateAsync(endpoint);
        }

        protected ConnectionFactory CreateConnectionFactory()
        {
            return new ConnectionFactory
            {
                LoggerFactory = CreateTestLoggerFactory(),
                MessageIdPolicyFactory = MessageIdPolicyFactory.GuidMessageIdPolicy
            };
        }

        protected ILoggerFactory CreateTestLoggerFactory()
        {
            return new TestLoggerFactory(_output);
        }

        protected static TestContainerHost CreateOpenedContainerHost(Endpoint endpoint = null, IHandler handler = null)
        {
            var host = CreateContainerHost(endpoint, handler);
            host.Open();
            return host;
        }
        
        protected static TestContainerHost CreateContainerHost(Endpoint endpoint = null, IHandler handler = null)
        {
            endpoint ??= GetUniqueEndpoint();
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

        protected static Task DisposeHostAndWaitUntilConnectionNotified(TestContainerHost host, IConnection connection)
        {
            var tcs = new TaskCompletionSource<bool>();
            var cts = new CancellationTokenSource(Timeout);
            cts.Token.Register(() => tcs.TrySetCanceled());
            connection.ConnectionClosed += (sender, args) =>
            {
                tcs.TrySetResult(true);
            };
            host.Dispose();
            return tcs.Task;
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
        
        protected static CancellationToken CancellationToken => new CancellationTokenSource(Timeout).Token;
    }
}