using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.InternalUtilities;
using ActiveMQ.Artemis.Client.UnitTests.Utils;
using Amqp.Framing;
using Amqp.Handler;
using Xunit;
using Xunit.Abstractions;

namespace ActiveMQ.Artemis.Client.UnitTests.AutoRecovering
{
    public class AutoRecoveringRequestReplyClientSpec : ActiveMQNetSpec
    {
        public AutoRecoveringRequestReplyClientSpec(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task Should_not_recreate_disposed_request_reply_clients()
        {
            var endpoint = GetUniqueEndpoint();
            var linksAttached = new CountdownEvent(2);
            var connectionRecovered = new ManualResetEvent(false);
            var testHandler = new TestHandler(@event =>
            {
                switch (@event.Id)
                {
                    case EventId.LinkRemoteOpen when @event.Context is Attach:
                        linksAttached.Signal();
                        break;
                }
            });

            var host1 = CreateOpenedContainerHost(endpoint, testHandler);

            var connection = await CreateConnection(endpoint);
            connection.ConnectionRecovered += (_, _) => connectionRecovered.Set();
            var rpcClient = await connection.CreateRequestReplyClientAsync();

            Assert.True(linksAttached.Wait(Timeout));
            await rpcClient.DisposeAsync();

            linksAttached.Reset();

            await DisposeHostAndWaitUntilConnectionNotified(host1, connection);

            var host2 = CreateOpenedContainerHost(endpoint, testHandler);

            Assert.True(connectionRecovered.WaitOne(Timeout));

            Assert.False(linksAttached.Wait(ShortTimeout));

            await DisposeUtil.DisposeAll(connection, host2);
        }
    }
}
