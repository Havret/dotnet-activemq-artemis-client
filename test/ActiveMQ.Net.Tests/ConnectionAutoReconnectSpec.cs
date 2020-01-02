using System;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Net.Tests.Utils;
using Amqp.Handler;
using Xunit;

namespace ActiveMQ.Net.Tests
{
    public class ConnectionAutoReconnectSpec
    {
        [Fact]
        public async Task Should_reconnect_when_broker_is_available_after_outage_is_over()
        {
            var address = AddressUtil.GetAddress();
            var connectionOpened = new ManualResetEvent(false);

            var testHandler = new TestHandler(@event =>
            {
                switch (@event.Id)
                {
                    case EventId.ConnectionRemoteOpen:
                        connectionOpened.Set();
                        break;
                }
            });

            using var host1 = new TestContainerHost(address, testHandler);
            host1.Open();

            var connection = await CreateConnection(address);
            Assert.NotNull(connection);
            Assert.True(connectionOpened.WaitOne(TimeSpan.FromSeconds(1)));

            host1.Dispose();

            connectionOpened.Reset();
            using var host2 = new TestContainerHost(address, testHandler);
            host2.Open();
            
            Assert.True(connectionOpened.WaitOne(TimeSpan.FromSeconds(10000)));
        }

        private static Task<IConnection> CreateConnection(string address)
        {
            var connectionFactory = new ConnectionFactory();
            return connectionFactory.CreateAsync(address);
        }
    }
}