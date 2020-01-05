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
        public async Task Should_be_able_to_produce_messages_when_connection_restored()
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
            var producer = connection.CreateProducer("a1");
            Assert.NotNull(producer);
            
            Assert.True(producerAttached.WaitOne(TimeSpan.FromSeconds(1)));
            producerAttached.Reset();
            
            host1.Dispose();
            
            using var host2 = CreateOpenedContainerHost(address, testHandler);
            var messageProcessor = host2.CreateMessageProcessor("a1");

            // wait until sender link is reattached
            Assert.True(producerAttached.WaitOne(TimeSpan.FromSeconds(1)));
            
            await producer.ProduceAsync(new Message("foo"));

            var message = messageProcessor.Dequeue(TimeSpan.FromSeconds(1));
            Assert.NotNull(message);
            Assert.Equal("foo", message.GetBody<string>());
        }
    }
}