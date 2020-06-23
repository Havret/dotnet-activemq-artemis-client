using System;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Exceptions;
using Xunit;
using Xunit.Abstractions;

namespace ActiveMQ.Artemis.Client.IntegrationTests.TopologyManagement
{
    public class DeclareQueueSpec : ActiveMQNetIntegrationSpec
    {
        public DeclareQueueSpec(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task Should_create_queue_when_queue_does_not_exist()
        {
            await using var connection = await CreateConnection();
            await using var topologyManager = await connection.CreateTopologyManagerAsync();

            var address = Guid.NewGuid().ToString();
            var queue = Guid.NewGuid().ToString();
            await topologyManager.CreateAddressAsync(address, RoutingType.Multicast, CancellationToken);
            await topologyManager.DeclareQueueAsync(new QueueConfiguration
            {
                Name = queue,
                Address = address,
                RoutingType = RoutingType.Multicast,
            }, CancellationToken);

            var queueNames = await topologyManager.GetQueueNamesAsync(CancellationToken);
            Assert.Contains(queue, queueNames);
        }

        [Fact]
        public async Task Should_update_queue_when_it_exists_but_has_different_configuration()
        {
            await using var connection = await CreateConnection();
            await using var topologyManager = await connection.CreateTopologyManagerAsync();

            var address = Guid.NewGuid().ToString();
            var queue = Guid.NewGuid().ToString();

            await topologyManager.CreateAddressAsync(address, RoutingType.Multicast, CancellationToken);
            await topologyManager.CreateQueueAsync(new QueueConfiguration
            {
                Name = queue,
                Address = address,
                RoutingType = RoutingType.Multicast,
                FilterExpression = "AMQPriority > 0" // TODO: Remove when Artemis 2.14.0 released
            }, CancellationToken);
            await topologyManager.DeclareQueueAsync(new QueueConfiguration
            {
                Name = queue,
                Address = address,
                RoutingType = RoutingType.Multicast,
                MaxConsumers = 1,
                FilterExpression = "AMQPriority > 0" // TODO: Remove when Artemis 2.14.0 released
            }, CancellationToken);

            // make sure that queue was updated and we can attach only 1 consumer 
            await using var _ = await connection.CreateConsumerAsync(address, queue, CancellationToken);
            await Assert.ThrowsAsync<CreateConsumerException>(async () => await connection.CreateConsumerAsync(address, queue));
        }

        [Fact]
        public async Task Throws_when_queue_configuration_is_null()
        {
            await using var connection = await CreateConnection();
            await using var topologyManager = await connection.CreateTopologyManagerAsync();
            await Assert.ThrowsAsync<ArgumentNullException>(() => topologyManager.DeclareQueueAsync(null));
        }
    }
}