using System.Threading;
using System.Threading.Tasks;
using Amqp.Handler;
using Xunit;

namespace ActiveMQ.Net.Tests
{
    public class ConnectionSpec
    {
        private readonly string _address = "amqp://guest:guest@localhost:15672";

        [Fact]
        public async Task Should_create_and_close_connection()
        {
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

            using var host = new TestContainerHost(_address, testHandler);
            host.Open();

            var connection = await CreateConnection(_address);
            await connection.DisposeAsync();

            Assert.True(connectionOpened.WaitOne());
            Assert.True(connectionClosed.WaitOne());
        }

        private static Task<IConnection> CreateConnection(string address)
        {
            var connectionFactory = new ConnectionFactory();
            return connectionFactory.CreateAsync(address);
        }
    }
}