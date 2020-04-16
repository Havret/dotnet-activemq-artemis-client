using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Net.Tests.Utils;
using Amqp.Framing;
using Amqp.Handler;
using Xunit;
using Xunit.Abstractions;

namespace ActiveMQ.Net.Tests.AutoRecovering
{
    public class AutoRecoveringProducerSpec : ActiveMQNetSpec
    {
        public AutoRecoveringProducerSpec(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task Should_be_able_to_SendAsync_message_when_connection_restored()
        {
            var (producer, messageProcessor, host, connection) = await CreateReattachedProducer();
            await producer.SendAsync(new Message("foo"));

            var message = messageProcessor.Dequeue(Timeout);
            Assert.NotNull(message);
            Assert.Equal("foo", message.GetBody<string>());

            await DisposeUtil.DisposeAll(producer, connection, host);
        }

        [Fact]
        public async Task Should_be_able_to_Send_message_when_connection_restored()
        {
            var (producer, messageProcessor, host, connection) = await CreateReattachedProducer();

            producer.Send(new Message("foo"));

            var message = messageProcessor.Dequeue(Timeout);
            Assert.NotNull(message);
            Assert.Equal("foo", message.GetBody<string>());
            
            await DisposeUtil.DisposeAll(producer, connection, host);
        }

        [Fact]
        public async Task Should_be_able_to_SendAsync_message_when_SendAsync_invoked_before_connection_restored()
        {
            var endpoint = GetUniqueEndpoint();

            var host1 = CreateOpenedContainerHost(endpoint);

            var connection = await CreateConnection(endpoint);
            var producer = await connection.CreateProducerAsync("a1", AddressRoutingType.Anycast);

            host1.Dispose();

            var cts = new CancellationTokenSource(Timeout);
            var produceTask = producer.SendAsync(new Message("foo"), cts.Token);

            var host2 = CreateOpenedContainerHost(endpoint);
            var messageProcessor = host2.CreateMessageProcessor("a1");

            await produceTask;

            var message = messageProcessor.Dequeue(Timeout);
            Assert.NotNull(message);
            Assert.Equal("foo", message.GetBody<string>());

            await DisposeUtil.DisposeAll(connection, host2);
        }

        [Fact]
        public async Task Should_be_able_to_Send_message_when_Send_invoked_before_connection_restored()
        {
            var endpoint = GetUniqueEndpoint();

            var host1 = CreateOpenedContainerHost(endpoint);

            var connectionClosed = new ManualResetEvent(false);
            var connection = await CreateConnection(endpoint);
            connection.ConnectionClosed += (_, args) => connectionClosed.Set();
            
            var producer = await connection.CreateProducerAsync("a1", AddressRoutingType.Anycast);

            host1.Dispose();
            
            // make sure that the connection was closed, as we don't want to send a message to host1 
            Assert.True(connectionClosed.WaitOne(Timeout));

            // run on another thread as we don't want to block here 
            var produceTask = Task.Run(() => producer.Send(new Message("foo")));

            var host2 = CreateContainerHost(endpoint);
            var messageProcessor = host2.CreateMessageProcessor("a1");
            host2.Open();

            await produceTask;

            var message = messageProcessor.Dequeue(Timeout);
            Assert.NotNull(message);
            Assert.Equal("foo", message.GetBody<string>());

            await DisposeUtil.DisposeAll(connection, host2);
        }

        private async Task<(IProducer producer, MessageProcessor messageProcessor, TestContainerHost host, IConnection connection)> CreateReattachedProducer()
        {
            var endpoint = GetUniqueEndpoint();
            var producerAttached = new ManualResetEvent(false);

            var testHandler = new TestHandler(@event =>
            {
                switch (@event.Id)
                {
                    case EventId.LinkRemoteOpen when @event.Context is Attach attach && attach.Role:
                        producerAttached.Set();
                        break;
                }
            });

            var host1 = CreateOpenedContainerHost(endpoint, testHandler);

            var connection = await CreateConnection(endpoint);
            var producer = await connection.CreateProducerAsync("a1", AddressRoutingType.Anycast);
            Assert.NotNull(producer);

            Assert.True(producerAttached.WaitOne(Timeout), "Producer failed to attach within specified timeout.");
            producerAttached.Reset();

            host1.Dispose();

            var host2 = CreateContainerHost(endpoint, testHandler);
            var messageProcessor = host2.CreateMessageProcessor("a1");
            host2.Open();

            // wait until sender link is reattached
            Assert.True(producerAttached.WaitOne(Timeout), "Producer failed to reattach within specified timeout.");
            return (producer, messageProcessor, host2, connection);
        }
    }
}