using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace ActiveMQ.Artemis.Client.IntegrationTests
{
    public class SharedDurableConsumerSpec : ActiveMQNetIntegrationSpec
    {
        public SharedDurableConsumerSpec(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task Should_create_shared_durable_consumer()
        {
            await using var connection = await CreateConnection();
            var address = Guid.NewGuid().ToString();
            var queue = Guid.NewGuid().ToString();
            var consumer = await connection.CreateConsumerAsync(new ConsumerConfiguration
            {
                Address = address,
                Queue = queue,
                Shared = true,
                Durable = true
            });

            await using var topologyManager = await connection.CreateTopologyManagerAsync();
            var queueNames = await topologyManager.GetQueueNamesAsync();
            Assert.Contains(queue, queueNames);

            await consumer.DisposeAsync();

            queueNames = await topologyManager.GetQueueNamesAsync();
            Assert.Contains(queue, queueNames);
        }

        [Fact]
        public async Task Should_consume_messages_from_shared_durable_queue()
        {
            await using var connection = await CreateConnection();
            var address = Guid.NewGuid().ToString();
            var queue = Guid.NewGuid().ToString();
            var consumer1 = await CreateSharedDurableConsumerAsync();
            var consumer2 = await CreateSharedDurableConsumerAsync();

            await using var producer = await connection.CreateProducerAsync(address, RoutingType.Multicast);

            await producer.SendAsync(new Message("foo1"));
            await producer.SendAsync(new Message("foo2"));

            var msg1 = await consumer1.ReceiveAsync();
            var msg2 = await consumer2.ReceiveAsync();

            Assert.Equal("foo1", msg1.GetBody<string>());
            Assert.Equal("foo2", msg2.GetBody<string>());

            // Close consumers without acknowledging the messages
            await consumer1.DisposeAsync();
            await consumer2.DisposeAsync();

            // Create a brand new consumer
            await using var consumer = await CreateSharedDurableConsumerAsync();

            // Make sure the that messages were persisted
            msg1 = await consumer.ReceiveAsync();
            msg2 = await consumer.ReceiveAsync();

            await consumer.AcceptAsync(msg1);
            await consumer.AcceptAsync(msg2);

            Assert.Equal("foo1", msg1.GetBody<string>());
            Assert.Equal("foo2", msg2.GetBody<string>());

            Task<IConsumer> CreateSharedDurableConsumerAsync() => connection.CreateConsumerAsync(new ConsumerConfiguration
            {
                Address = address,
                Queue = queue,
                Shared = true,
                Durable = true
            });
        }

        [Fact]
        public async Task Should_attach_shared_durable_consumer_to_pre_configured_shared_durable_queue()
        {
            await using var connection = await CreateConnection();
            await using var topologyManager = await connection.CreateTopologyManagerAsync();

            var address = Guid.NewGuid().ToString();
            var queue = Guid.NewGuid().ToString();
            await topologyManager.DeclareQueueAsync(new QueueConfiguration
            {
                Address = address,
                RoutingType = RoutingType.Multicast,
                Name = queue,
                Durable = true,
                MaxConsumers = -1,
                AutoCreateAddress = true
            });
            
            await using var producer = await connection.CreateProducerAsync(address, RoutingType.Multicast);
            await producer.SendAsync(new Message("foo"));

            await using var consumer = await connection.CreateConsumerAsync(new ConsumerConfiguration
            {
                Address = address,
                Queue = queue,
                Shared = true,
                Durable = true
            });
            
            var msg = await consumer.ReceiveAsync();
            Assert.Equal("foo", msg.GetBody<string>());
        }
    }
}