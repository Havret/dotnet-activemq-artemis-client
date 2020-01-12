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
            var address = GetUniqueAddress();

            var host1 = CreateOpenedContainerHost(address);

            var connection = await CreateConnection(address);
            var producer = await connection.CreateProducer("a1");

            host1.Dispose();

            var cts = new CancellationTokenSource(Timeout);
            var produceTask = producer.SendAsync(new Message("foo"), cts.Token);

            var host2 = CreateOpenedContainerHost(address);
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
            var address = GetUniqueAddress();

            var host1 = CreateOpenedContainerHost(address);

            var connection = await CreateConnection(address);
            var producer = await connection.CreateProducer("a1");

            host1.Dispose();

            // run on another thread as we don't want to block here 
            var produceTask = Task.Run(() => producer.Send(new Message("foo")));

            using var host2 = CreateOpenedContainerHost(address);
            var messageProcessor = host2.CreateMessageProcessor("a1");

            await produceTask;

            var message = messageProcessor.Dequeue(Timeout);
            Assert.NotNull(message);
            Assert.Equal("foo", message.GetBody<string>());
        }

        private async Task<(IProducer producer, MessageProcessor messageProcessor, TestContainerHost host, IConnection connection)> CreateReattachedProducer()
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

            Assert.True(producerAttached.WaitOne(Timeout), "Producer failed to attach within specified timeout.");
            producerAttached.Reset();

            host1.Dispose();

            var host2 = CreateOpenedContainerHost(address, testHandler);
            var messageProcessor = host2.CreateMessageProcessor("a1");

            // wait until sender link is reattached
            Assert.True(producerAttached.WaitOne(Timeout), "Producer failed to reattach within specified timeout.");
            return (producer, messageProcessor, host2, connection);
        }
    }
}