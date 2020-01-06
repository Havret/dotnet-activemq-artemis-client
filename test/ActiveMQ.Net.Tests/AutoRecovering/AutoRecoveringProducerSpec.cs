using System;
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
        public async Task Should_be_able_to_ProduceAsync_message_when_connection_restored()
        {
            var (producer, messageProcessor, host, connection) = await CreateReattachedConsumer();
            await producer.ProduceAsync(new Message("foo"));

            var message = messageProcessor.Dequeue(TimeSpan.FromSeconds(1));
            Assert.NotNull(message);
            Assert.Equal("foo", message.GetBody<string>());

            await DisposeUtil.DisposeAll(producer, connection, host);
        }

        [Fact]
        public async Task Should_be_able_to_Produce_message_when_connection_restored()
        {
            var (producer, messageProcessor, host, connection) = await CreateReattachedConsumer();

            producer.Produce(new Message("foo"));

            var message = messageProcessor.Dequeue(TimeSpan.FromSeconds(1));
            Assert.NotNull(message);
            Assert.Equal("foo", message.GetBody<string>());
            
            await DisposeUtil.DisposeAll(producer, connection, host);
        }

        [Fact]
        public async Task Should_be_able_to_ProduceAsync_message_when_ProduceAsync_invoked_before_connection_restored()
        {
            var address = GetUniqueAddress();

            var host1 = CreateOpenedContainerHost(address);

            var connection = await CreateConnection(address);
            var producer = await connection.CreateProducer("a1");

            host1.Dispose();

            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var produceTask = producer.ProduceAsync(new Message("foo"), cts.Token);

            using var host2 = CreateOpenedContainerHost(address);
            var messageProcessor = host2.CreateMessageProcessor("a1");

            await produceTask;

            var message = messageProcessor.Dequeue(TimeSpan.FromSeconds(1));
            Assert.NotNull(message);
            Assert.Equal("foo", message.GetBody<string>());
        }

        [Fact]
        public async Task Should_be_able_to_Produce_message_when_Produce_invoked_before_connection_restored()
        {
            var address = GetUniqueAddress();

            var host1 = CreateOpenedContainerHost(address);

            var connection = await CreateConnection(address);
            var producer = await connection.CreateProducer("a1");

            host1.Dispose();

            // run on another thread as we don't want to block here 
            var produceTask = Task.Run(() => producer.Produce(new Message("foo")));

            using var host2 = CreateOpenedContainerHost(address);
            var messageProcessor = host2.CreateMessageProcessor("a1");

            await produceTask;

            var message = messageProcessor.Dequeue(TimeSpan.FromSeconds(1));
            Assert.NotNull(message);
            Assert.Equal("foo", message.GetBody<string>());
        }

        private async Task<(IProducer producer, MessageProcessor messageProcessor, TestContainerHost host, IConnection connection)> CreateReattachedConsumer()
        {
            var address = GetUniqueAddress();
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

            var host1 = CreateOpenedContainerHost(address, testHandler);

            var connection = await CreateConnection(address);
            var producer = await connection.CreateProducer("a1");
            Assert.NotNull(producer);

            Assert.True(producerAttached.WaitOne(TimeSpan.FromSeconds(1)));
            producerAttached.Reset();

            host1.Dispose();

            var host2 = CreateOpenedContainerHost(address, testHandler);
            var messageProcessor = host2.CreateMessageProcessor("a1");

            // wait until sender link is reattached
            Assert.True(producerAttached.WaitOne(TimeSpan.FromSeconds(1)));
            return (producer, messageProcessor, host2, connection);
        }
    }
}