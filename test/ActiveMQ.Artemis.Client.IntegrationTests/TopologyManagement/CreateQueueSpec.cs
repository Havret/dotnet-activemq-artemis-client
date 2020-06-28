using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace ActiveMQ.Artemis.Client.IntegrationTests.TopologyManagement
{
    public class CreateQueueSpec : ActiveMQNetIntegrationSpec
    {
        public CreateQueueSpec(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task Should_create_queue()
        {
            await using var connection = await CreateConnection();
            await using var topologyManager = await connection.CreateTopologyManagerAsync();

            var address = Guid.NewGuid().ToString();
            await topologyManager.CreateAddressAsync(address, RoutingType.Multicast, CancellationToken);

            var queueName = Guid.NewGuid().ToString();
            await topologyManager.CreateQueueAsync(new QueueConfiguration
            {
                Name = queueName,
                RoutingType = RoutingType.Multicast,
                Address = address,
                Durable = true,
                Exclusive = false,
                GroupRebalance = false,
                GroupBuckets = 64,
                MaxConsumers = -1,
                AutoCreateAddress = false,
                PurgeOnNoConsumers = false
            }, CancellationToken);
        }

        [Fact]
        public async Task Should_create_queue_and_address()
        {
            await using var connection = await CreateConnection();
            await using var topologyManager = await connection.CreateTopologyManagerAsync();

            var address = Guid.NewGuid().ToString();

            var queueName = Guid.NewGuid().ToString();
            await topologyManager.CreateQueueAsync(new QueueConfiguration
            {
                Name = queueName,
                RoutingType = RoutingType.Multicast,
                Address = address,
                AutoCreateAddress = true
            }, CancellationToken);
        }

        [Fact]
        public async Task Throws_when_queue_already_exists()
        {
            await using var connection = await CreateConnection();
            await using var topologyManager = await connection.CreateTopologyManagerAsync();

            var queueConfiguration = new QueueConfiguration
            {
                Name = Guid.NewGuid().ToString(),
                RoutingType = RoutingType.Multicast,
                Address = Guid.NewGuid().ToString(),
                AutoCreateAddress = true
            };

            await topologyManager.CreateQueueAsync(queueConfiguration, CancellationToken);

            await Assert.ThrowsAsync<InvalidOperationException>(async () => await topologyManager.CreateQueueAsync(queueConfiguration, CancellationToken));
        }

        [Fact]
        public async Task Throws_when_address_does_not_exist_and_AutoCreateAddress_set_to_false()
        {
            await using var connection = await CreateConnection();
            await using var topologyManager = await connection.CreateTopologyManagerAsync();

            var queueConfiguration = new QueueConfiguration
            {
                Name = Guid.NewGuid().ToString(),
                RoutingType = RoutingType.Multicast,
                Address = Guid.NewGuid().ToString(),
                AutoCreateAddress = false
            };

            await Assert.ThrowsAsync<InvalidOperationException>(async () => await topologyManager.CreateQueueAsync(queueConfiguration, CancellationToken));
        }

        [Fact]
        public async Task Should_create_queue_with_filter_expression()
        {
            await using var connection = await CreateConnection();
            await using var topologyManager = await connection.CreateTopologyManagerAsync();

            var address = Guid.NewGuid().ToString();
            var queue = Guid.NewGuid().ToString();
            await topologyManager.CreateQueueAsync(new QueueConfiguration
            {
                Address = address,
                RoutingType = RoutingType.Multicast,
                Name = queue,
                AutoCreateAddress = true,
                FilterExpression = "AMQPriority = 9"
            }, CancellationToken);

            var consumer = await connection.CreateConsumerAsync(address, queue, CancellationToken);
            var producer = await connection.CreateProducerAsync(address, RoutingType.Multicast);

            await producer.SendAsync(new Message("foo1") { Priority = 4 });
            await producer.SendAsync(new Message("foo2") { Priority = 9 });
            
            Assert.Equal("foo2", (await consumer.ReceiveAsync(CancellationToken)).GetBody<string>());
        }
    }
}