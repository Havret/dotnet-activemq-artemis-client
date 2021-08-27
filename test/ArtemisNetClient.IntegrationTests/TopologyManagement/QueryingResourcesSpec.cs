using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace ActiveMQ.Artemis.Client.IntegrationTests.TopologyManagement
{
    public class QueryingResourcesSpec : ActiveMQNetIntegrationSpec
    {
        public QueryingResourcesSpec(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task Should_get_address_names()
        {
            await using var connection = await CreateConnection();
            await using var topologyManager = await connection.CreateTopologyManagerAsync();

            var addressNames = await topologyManager.GetAddressNamesAsync();
            Assert.Contains("DLQ", addressNames);
        }

        [Fact]
        public async Task Should_get_queue_names()
        {
            await using var connection = await CreateConnection();
            await using var topologyManager = await connection.CreateTopologyManagerAsync();

            var queueNames = await topologyManager.GetQueueNamesAsync();
            Assert.Contains("DLQ", queueNames);
        }
    }
}