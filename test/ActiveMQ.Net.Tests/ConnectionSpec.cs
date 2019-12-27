using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Net.Tests.Utils;
using Amqp.Handler;
using Xunit;

namespace ActiveMQ.Net.Tests
{
    public class ConnectionSpec
    {
        [Fact]
        public async Task Should_create_and_close_connection()
        {
            var address = AddressUtil.GetAddress();
            var connectionOpened = new ManualResetEvent(false);
            var connectionClosed = new ManualResetEvent(false);

            var testHandler = new TestHandler(@event =>
            {
                switch (@event.Id)
                {
                    case EventId.ConnectionRemoteOpen:
                        connectionOpened.Set();
                        break;
                    case EventId.ConnectionRemoteClose:
                        connectionClosed.Set();
                        break;
                }
            });

            using var host = new TestContainerHost(address, testHandler);
            host.Open();

            var connection = await CreateConnection(address);
            await connection.DisposeAsync();

            Assert.True(connectionOpened.WaitOne());
            Assert.True(connectionClosed.WaitOne());
        }

        [Fact]
        public async Task New_connection_should_implicitly_open_new_session()
        {
            var address = AddressUtil.GetAddress();
            var sessionOpened = new ManualResetEvent(false);

            var testHandler = new TestHandler(@event =>
            {
                switch (@event.Id)
                {
                    case EventId.SessionRemoteOpen:
                        sessionOpened.Set();
                        break;
                }
            });

            using var host = new TestContainerHost(address, testHandler);
            host.Open();

            await using var connection = await CreateConnection(address);

            Assert.True(sessionOpened.WaitOne());
        }

        private static Task<IConnection> CreateConnection(string address)
        {
            var connectionFactory = new ConnectionFactory();
            return connectionFactory.CreateAsync(address);
        }
    }
}