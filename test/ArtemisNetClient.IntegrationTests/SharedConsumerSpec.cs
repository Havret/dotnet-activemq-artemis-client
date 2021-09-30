using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace ActiveMQ.Artemis.Client.IntegrationTests
{
    public class SharedConsumerSpec : ActiveMQNetIntegrationSpec
    {
        public SharedConsumerSpec(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task Should_create_shared_volatile_consumer()
        {
            await using var connection = await CreateConnection();
            var address = Guid.NewGuid().ToString();
            var queue = Guid.NewGuid().ToString();
            var consumer = await connection.CreateConsumerAsync(new ConsumerConfiguration
            {
                Address = address,
                Queue = queue,
                Shared = true
            });

            await using var topologyManager = await connection.CreateTopologyManagerAsync();
            var queueNames = await topologyManager.GetQueueNamesAsync();
            Assert.Contains($"{queue}:shared-volatile:global", queueNames);

            await consumer.DisposeAsync();

            queueNames = await topologyManager.GetQueueNamesAsync();
            Assert.DoesNotContain($"{queue}:shared-volatile:global", queueNames);
        }

        [Fact]
        public async Task Should_consume_messages_from_shared_volatile_queue()
        {
            await using var connection = await CreateConnection();
            var address = Guid.NewGuid().ToString();
            var queue = Guid.NewGuid().ToString();
            await using var consumer1 = await connection.CreateConsumerAsync(new ConsumerConfiguration
            {
                Address = address,
                Queue = queue,
                Shared = true
            });
            await using var consumer2 = await connection.CreateConsumerAsync(new ConsumerConfiguration
            {
                Address = address,
                Queue = queue,
                Shared = true
            });
            
            await using var producer = await connection.CreateProducerAsync(address, RoutingType.Multicast);
            
            await producer.SendAsync(new Message("foo1"));
            await producer.SendAsync(new Message("foo2"));
            
            var msg1 = await consumer1.ReceiveAsync();
            var msg2 = await consumer2.ReceiveAsync();

            Assert.Equal("foo1", msg1.GetBody<string>());
            Assert.Equal("foo2", msg2.GetBody<string>());
        }
    }
}