using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace ActiveMQ.Net.Tests
{
    public class CreateAnonymousProducerSpec : ActiveMQNetSpec
    {
        public CreateAnonymousProducerSpec(ITestOutputHelper output) : base(output)
        {
        }
        
        [Fact]
        public async Task Throws_when_created_with_null_configuration()
        {
            var endpoint = GetUniqueEndpoint();
            using var host = CreateOpenedContainerHost(endpoint);
            await using var connection = await CreateConnection(endpoint);

            await Assert.ThrowsAsync<ArgumentNullException>(() => connection.CreateAnonymousProducer(null));
        }
    }
}