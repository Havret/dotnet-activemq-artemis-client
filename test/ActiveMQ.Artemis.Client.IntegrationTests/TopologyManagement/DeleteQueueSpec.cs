using System;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Exceptions;
using Xunit;
using Xunit.Abstractions;

namespace ActiveMQ.Artemis.Client.IntegrationTests.TopologyManagement
{
    public class DeleteQueueSpec : ActiveMQNetIntegrationSpec
    {
        public DeleteQueueSpec(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task Should_delete_queue()
        {
            await using var connection = await CreateConnection();
            await using var topologyManager = await connection.CreateTopologyManagerAsync();

            var address = Guid.NewGuid().ToString();
            var queue = Guid.NewGuid().ToString();
            await topologyManager.CreateQueueAsync(new QueueConfiguration
            {
                Address = address,
                RoutingType = RoutingType.Anycast,
                Name = queue,
                AutoCreateAddress = true
            });

            await topologyManager.DeleteQueueAsync(queue, cancellationToken: CancellationToken);

            var queueNames = await topologyManager.GetQueueNamesAsync(CancellationToken);
            Assert.DoesNotContain(queue, queueNames);
        }

        [Fact]
        public async Task Throws_when_queue_does_not_exist()
        {
            await using var connection = await CreateConnection();
            await using var topologyManager = await connection.CreateTopologyManagerAsync();
            
            var queue = Guid.NewGuid().ToString();

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => topologyManager.DeleteQueueAsync(queue, cancellationToken: CancellationToken));
            Assert.Contains($"AMQ229017: Queue {queue} does not exist", exception.Message);
        }

        [Fact]
        public async Task Should_delete_queue_alongside_with_auto_created_address()
        {
            await using var connection = await CreateConnection();
            await using var topologyManager = await connection.CreateTopologyManagerAsync();

            var address = Guid.NewGuid().ToString();
            var queue = Guid.NewGuid().ToString();
            await topologyManager.CreateQueueAsync(new QueueConfiguration
            {
                Address = address,
                RoutingType = RoutingType.Anycast,
                Name = queue,
                AutoCreateAddress = true
            });

            await topologyManager.DeleteQueueAsync(queue, autoDeleteAddress: true, cancellationToken: CancellationToken);

            var queueNames = await topologyManager.GetQueueNamesAsync(CancellationToken);
            Assert.DoesNotContain(queue, queueNames);
            
            var addressNames = await topologyManager.GetAddressNamesAsync(CancellationToken);
            Assert.DoesNotContain(address, addressNames);
        }
        
        [Fact]
        public async Task Should_not_delete_address_when_not_auto_created()
        {
            await using var connection = await CreateConnection();
            await using var topologyManager = await connection.CreateTopologyManagerAsync();

            var address = Guid.NewGuid().ToString();
            var queue = Guid.NewGuid().ToString();
            await topologyManager.CreateAddressAsync(address, RoutingType.Anycast, CancellationToken);
            await topologyManager.CreateQueueAsync(new QueueConfiguration
            {
                Address = address,
                RoutingType = RoutingType.Anycast,
                Name = queue,
            }, CancellationToken);

            await topologyManager.DeleteQueueAsync(queue, autoDeleteAddress: true, cancellationToken: CancellationToken);

            var queueNames = await topologyManager.GetQueueNamesAsync(CancellationToken);
            Assert.DoesNotContain(queue, queueNames);
            
            var addressNames = await topologyManager.GetAddressNamesAsync(CancellationToken);
            Assert.Contains(address, addressNames);
        }

        [Fact]
        public async Task Should_delete_queue_and_remove_consumers()
        {
            // TODO: Should be changed to CreateConnection when https://github.com/Havret/dotnet-activemq-artemis-client/issues/185 fixed 
            await using var connection = await CreateConnectionWithoutAutomaticRecovery();
            await using var topologyManager = await connection.CreateTopologyManagerAsync();

            var address = Guid.NewGuid().ToString();
            var queue = Guid.NewGuid().ToString();
            await topologyManager.CreateQueueAsync(new QueueConfiguration
            {
                Address = address,
                RoutingType = RoutingType.Anycast,
                Name = queue,
                AutoCreateAddress = true
            }, CancellationToken);

            var consumer = await connection.CreateConsumerAsync(address, queue, CancellationToken);

            await topologyManager.DeleteQueueAsync(queue, removeConsumers: true, cancellationToken: CancellationToken);

            var queueNames = await topologyManager.GetQueueNamesAsync(CancellationToken);
            Assert.DoesNotContain(queue, queueNames);

            await Assert.ThrowsAsync<ConsumerClosedException>(async () => await consumer.ReceiveAsync(CancellationToken));
        }
    }
}