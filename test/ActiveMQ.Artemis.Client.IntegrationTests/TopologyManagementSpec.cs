using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace ActiveMQ.Artemis.Client.IntegrationTests
{
    public class TopologyManagementSpec : ActiveMQNetIntegrationSpec
    {
        public TopologyManagementSpec(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task Should_get_address_names()
        {
            await using var connection = await CreateConnection();
            await using var topologyManager = await connection.CreateTopologyManager();

            var addressNames = await topologyManager.GetAddressNames();
            Assert.Contains("DLQ", addressNames);
        }

        [Fact]
        public async Task Should_get_queue_names()
        {
            await using var connection = await CreateConnection();
            await using var topologyManager = await connection.CreateTopologyManager();

            var queueNames = await topologyManager.GetQueueNames();
            Assert.Contains("DLQ", queueNames);
        }

        [Theory]
        [InlineData(new[] { RoutingType.Multicast })]
        [InlineData(new[] { RoutingType.Anycast })]
        [InlineData(new[] { RoutingType.Anycast, RoutingType.Multicast })]
        public async Task Should_create_address(RoutingType[] routingTypes)
        {
            await using var connection = await CreateConnection();
            await using var topologyManager = await connection.CreateTopologyManager();

            var address = Guid.NewGuid().ToString();
            await topologyManager.CreateAddress(address, routingTypes, CancellationToken);

            var addressNames = await topologyManager.GetAddressNames(CancellationToken);
            Assert.Contains(address, addressNames);
        }

        [Fact]
        public async Task Throws_when_address_already_exists()
        {
            await using var connection = await CreateConnection();
            await using var topologyManager = await connection.CreateTopologyManager();

            var address = Guid.NewGuid().ToString();
            await topologyManager.CreateAddress(address, RoutingType.Anycast, CancellationToken);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await topologyManager.CreateAddress(address, RoutingType.Multicast, CancellationToken));
            Assert.Contains("Address already exists", exception.Message);
        }

        [Fact]
        public async Task Should_create_queue()
        {
            await using var connection = await CreateConnection();
            await using var topologyManager = await connection.CreateTopologyManager();

            var address = Guid.NewGuid().ToString();
            await topologyManager.CreateAddress(address, RoutingType.Multicast, CancellationToken);

            var queueName = Guid.NewGuid().ToString();
            await topologyManager.CreateQueue(new QueueConfiguration
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
            await using var topologyManager = await connection.CreateTopologyManager();

            var address = Guid.NewGuid().ToString();

            var queueName = Guid.NewGuid().ToString();
            await topologyManager.CreateQueue(new QueueConfiguration
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
            await using var topologyManager = await connection.CreateTopologyManager();

            var queueConfiguration = new QueueConfiguration
            {
                Name = Guid.NewGuid().ToString(),
                RoutingType = RoutingType.Multicast,
                Address = Guid.NewGuid().ToString(),
                AutoCreateAddress = true
            };

            await topologyManager.CreateQueue(queueConfiguration, CancellationToken);
            
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await topologyManager.CreateQueue(queueConfiguration, CancellationToken));
        }

        [Fact]
        public async Task Throws_when_address_does_not_exist_and_AutoCreateAddress_set_to_false()
        {
            await using var connection = await CreateConnection();
            await using var topologyManager = await connection.CreateTopologyManager();

            var queueConfiguration = new QueueConfiguration
            {
                Name = Guid.NewGuid().ToString(),
                RoutingType = RoutingType.Multicast,
                Address = Guid.NewGuid().ToString(),
                AutoCreateAddress = false
            };
            
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await topologyManager.CreateQueue(queueConfiguration, CancellationToken));
        }
    }
}