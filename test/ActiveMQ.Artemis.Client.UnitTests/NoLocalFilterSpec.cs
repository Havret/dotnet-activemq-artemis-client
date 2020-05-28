using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace ActiveMQ.Artemis.Client.UnitTests
{
    public class NoLocalFilterSpec : ActiveMQNetSpec
    {
        public NoLocalFilterSpec(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task Throws_when_NoLocalFilter_used_with_Anycast_routing_type()
        {
            using var host = CreateOpenedContainerHost();
            await using var connection = await CreateConnection(host.Endpoint);

            await Assert.ThrowsAsync<ArgumentException>(async () => await connection.CreateConsumerAsync(new ConsumerConfiguration
            {
                Address = "a1",
                RoutingType = RoutingType.Anycast,
                NoLocalFilter = true
            }));
        }
    }
}