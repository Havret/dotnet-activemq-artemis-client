using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace ActiveMQ.Artemis.Client.IntegrationTests.TopologyManagement
{
    public class DeclareAddressSpec : ActiveMQNetIntegrationSpec
    {
        public DeclareAddressSpec(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task Should_create_new_address_when_address_does_not_exist()
        {
            await using var connection = await CreateConnection();
            await using var topologyManager = await connection.CreateTopologyManagerAsync();

            var address = Guid.NewGuid().ToString();
            await topologyManager.DeclareAddressAsync(address, RoutingType.Multicast, CancellationToken);

            await Assert.ThrowsAsync<InvalidOperationException>(async () => await topologyManager.CreateAddressAsync(address, RoutingType.Multicast, CancellationToken));
        }

        [Fact]
        public async Task Should_do_nothing_when_address_exists_and_has_the_same_routing_type()
        {
            await using var connection = await CreateConnection();
            await using var topologyManager = await connection.CreateTopologyManagerAsync();

            var address = Guid.NewGuid().ToString();
            await topologyManager.CreateAddressAsync(address, RoutingType.Multicast, CancellationToken);

            await topologyManager.DeclareAddressAsync(address, RoutingType.Multicast, CancellationToken);

            // ensure that we can create a queue on the address using Multicast routing type
            await topologyManager.CreateQueueAsync(new QueueConfiguration
            {
                Address = address,
                RoutingType = RoutingType.Multicast,
                Name = Guid.NewGuid().ToString(),
            });

            // ensure that we cannot create a queue on the address using Anycast routing type
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await topologyManager.CreateQueueAsync(new QueueConfiguration
            {
                Address = address,
                RoutingType = RoutingType.Anycast,
                Name = Guid.NewGuid().ToString(),
            }));
        }

        [Fact]
        public async Task Should_update_address_when_it_exists_but_has_different_routing_type()
        {
            await using var connection = await CreateConnection();
            await using var topologyManager = await connection.CreateTopologyManagerAsync();

            var address = Guid.NewGuid().ToString();
            await topologyManager.CreateAddressAsync(address, RoutingType.Multicast, CancellationToken);

            await topologyManager.DeclareAddressAsync(address, new[] { RoutingType.Anycast, RoutingType.Multicast }, CancellationToken);

            // ensure that address routing type was updated and we can create a queue on this address using the new routing type
            await topologyManager.CreateQueueAsync(new QueueConfiguration
            {
                Address = address,
                RoutingType = RoutingType.Anycast,
                Name = Guid.NewGuid().ToString(),
            });
        }
    }
}