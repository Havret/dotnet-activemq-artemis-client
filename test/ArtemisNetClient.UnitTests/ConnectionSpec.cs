using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.UnitTests.Utils;
using Amqp.Handler;
using Xunit;
using Xunit.Abstractions;

namespace ActiveMQ.Artemis.Client.UnitTests
{
    public class ConnectionSpec : ActiveMQNetSpec
    {
        public ConnectionSpec(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task Should_create_and_close_connection()
        {
            var endpoint = GetUniqueEndpoint();
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

            using var host = CreateOpenedContainerHost(endpoint, testHandler);

            var connection = await CreateConnection(endpoint);
            await connection.DisposeAsync();

            Assert.True(connectionOpened.WaitOne());
            Assert.True(connectionClosed.WaitOne());
        }

        [Fact]
        public async Task Should_raise_ConnectionClosed_when_connection_closed_by_remote_peer()
        {
            var endpoint = GetUniqueEndpoint();
            var connectionClosedTcs = new TaskCompletionSource<(object, ConnectionClosedEventArgs)>(Timeout);

            var host = CreateOpenedContainerHost(endpoint);

            await using var connection = await CreateConnectionWithoutAutomaticRecovery(endpoint);

            connection.ConnectionClosed += (s, args) => { connectionClosedTcs.TrySetResult((s, args)); };

            // Explicitly dispose host to trigger notification indicating that remote peer has closed the connection. 
            host.Dispose();

            var (sender, connectionClosedEventArgs) = await connectionClosedTcs.Task;
            Assert.Same(sender, connection);
            Assert.True(connectionClosedEventArgs.ClosedByPeer, "Connection not closed by remote peer.");
        }

        [Fact]
        public async Task Should_raise_ConnectionClosed_when_event_handler_added_after_connection_was_already_closed()
        {
            var endpoint = GetUniqueEndpoint();
            var connectionClosed = new ManualResetEvent(false);

            using var host = CreateOpenedContainerHost(endpoint);

            var connection = await CreateConnectionWithoutAutomaticRecovery(endpoint);
            await connection.DisposeAsync();

            connection.ConnectionClosed += (_, _) =>
            {
                connectionClosed.Set();
            };

            Assert.True(connectionClosed.WaitOne(Timeout));
            Assert.False(connection.IsOpened);
        }

        [Fact]
        public async Task Should_toggle_IsOpened_property_to_false_when_connection_closed_by_remote_peer()
        {
            var endpoint = GetUniqueEndpoint();
            var connectionClosed = new ManualResetEvent(false);

            var testHandler = new TestHandler(@event =>
            {
                switch (@event.Id)
                {
                    case EventId.ConnectionRemoteClose:
                        connectionClosed.Set();
                        break;
                }
            });

            var host = CreateOpenedContainerHost(endpoint, testHandler);

            var connection = await CreateConnectionWithoutAutomaticRecovery(endpoint);

            // Explicitly dispose host to trigger notification indicating that remote peer closed the connection. 
            host.Dispose();

            Assert.True(connectionClosed.WaitOne());
            Assert.False(connection.IsOpened);
        }

        [Fact]
        public async Task Should_toggle_IsOpened_property_to_false_when_connection_closed()
        {
            var endpoint = GetUniqueEndpoint();
            var connectionClosed = new ManualResetEvent(false);

            var testHandler = new TestHandler(@event =>
            {
                switch (@event.Id)
                {
                    case EventId.ConnectionRemoteClose:
                        connectionClosed.Set();
                        break;
                }
            });

            using var host = CreateOpenedContainerHost(endpoint, testHandler);

            var connection = await CreateConnectionWithoutAutomaticRecovery(endpoint);
            await connection.DisposeAsync();

            Assert.True(connectionClosed.WaitOne());
            Assert.False(connection.IsOpened);
        }
    }
}