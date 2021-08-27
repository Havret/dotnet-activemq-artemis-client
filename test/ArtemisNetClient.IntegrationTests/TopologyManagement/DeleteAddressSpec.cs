using System;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Exceptions;
using Xunit;
using Xunit.Abstractions;

namespace ActiveMQ.Artemis.Client.IntegrationTests.TopologyManagement
{
    public class DeleteAddressSpec : ActiveMQNetIntegrationSpec
    {
        public DeleteAddressSpec(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task Should_delete_address()
        {
            await using var connection = await CreateConnection();
            await using var topologyManager = await connection.CreateTopologyManagerAsync();

            var address = Guid.NewGuid().ToString();
            await topologyManager.CreateAddressAsync(address, RoutingType.Multicast, CancellationToken);

            await topologyManager.DeleteAddressAsync(address, cancellationToken: CancellationToken);

            var addressNames = await topologyManager.GetAddressNamesAsync(CancellationToken);
            Assert.DoesNotContain(address, addressNames);
        }

        [Fact]
        public async Task Throws_when_address_does_not_exist()
        {
            await using var connection = await CreateConnection();
            await using var topologyManager = await connection.CreateTopologyManagerAsync();
            
            var address = Guid.NewGuid().ToString();

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => topologyManager.DeleteAddressAsync(address, cancellationToken: CancellationToken));
            Assert.Contains($"AMQ229203: Address Does Not Exist: {address}", exception.Message);
        }

        [Fact]
        public async Task Should_remove_all_queues_and_consumers_when_address_deleted_with_force_flag()
        {
            await using var connection = await CreateConnection();
            await using var topologyManager = await connection.CreateTopologyManagerAsync();

            var address = Guid.NewGuid().ToString();
            var queue = Guid.NewGuid().ToString();
            await topologyManager.CreateAddressAsync(address, RoutingType.Multicast, CancellationToken);
            await topologyManager.CreateQueueAsync(new QueueConfiguration
            {
                Address = address,
                RoutingType = RoutingType.Multicast,
                Name = queue,
            }, CancellationToken);
            
            var consumer = await connection.CreateConsumerAsync(address, queue, CancellationToken);

            await topologyManager.DeleteAddressAsync(address, force: true, cancellationToken: CancellationToken);

            var addressNames = await topologyManager.GetAddressNamesAsync(CancellationToken);
            Assert.DoesNotContain(address, addressNames);
            
            var queueNames = await topologyManager.GetQueueNamesAsync(CancellationToken);
            Assert.DoesNotContain(queue, queueNames);

            await Assert.ThrowsAsync<ConsumerClosedException>(async () => await consumer.ReceiveAsync(CancellationToken));
        }
    }
}