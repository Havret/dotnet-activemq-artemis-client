using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace ActiveMQ.Artemis.Client.IntegrationTests.TopologyManagement
{
    public class CreateAddressSpec : ActiveMQNetIntegrationSpec
    {
        public CreateAddressSpec(ITestOutputHelper output) : base(output)
        {
        }
        
        [Theory]
        [InlineData(new[] { RoutingType.Multicast })]
        [InlineData(new[] { RoutingType.Anycast })]
        [InlineData(new[] { RoutingType.Anycast, RoutingType.Multicast })]
        public async Task Should_create_address(RoutingType[] routingTypes)
        {
            await using var connection = await CreateConnection();
            await using var topologyManager = await connection.CreateTopologyManagerAsync();

            var address = Guid.NewGuid().ToString();
            await topologyManager.CreateAddressAsync(address, routingTypes, CancellationToken);

            var addressNames = await topologyManager.GetAddressNamesAsync(CancellationToken);
            Assert.Contains(address, addressNames);
        }
        
        [Fact]
        public async Task Throws_when_address_already_exists()
        {
            await using var connection = await CreateConnection();
            await using var topologyManager = await connection.CreateTopologyManagerAsync();

            var address = Guid.NewGuid().ToString();
            await topologyManager.CreateAddressAsync(address, RoutingType.Anycast, CancellationToken);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await topologyManager.CreateAddressAsync(address, RoutingType.Multicast, CancellationToken));
            Assert.Contains("Address already exists", exception.Message);
        }
    }
}