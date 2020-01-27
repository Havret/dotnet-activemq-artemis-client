using System.Linq;
using System.Threading.Tasks;
using ActiveMQ.Net.AutoRecovering;
using ActiveMQ.Net.Exceptions;
using Xunit;
using Xunit.Abstractions;

namespace ActiveMQ.Net.Tests
{
    public class ConnectionFactorySpec : ActiveMQNetSpec
    {
        public ConnectionFactorySpec(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task Should_create_connection()
        {
            var endpoint = GetUniqueEndpoint();

            using var host = CreateOpenedContainerHost(endpoint);

            var connectionFactory = new ConnectionFactory { LoggerFactory = CreateTestLoggerFactory(), AutomaticRecoveryEnabled = false };
            await using var connection = await connectionFactory.CreateAsync(endpoint);
            Assert.IsType<Connection>(connection);
        }

        [Fact]
        public async Task Should_create_auto_recovering_connection()
        {
            var endpoint = GetUniqueEndpoint();

            using var host = CreateOpenedContainerHost(endpoint);

            var connectionFactory = new ConnectionFactory { LoggerFactory = CreateTestLoggerFactory(), AutomaticRecoveryEnabled = true };
            await using var connection = await connectionFactory.CreateAsync(endpoint);
            Assert.IsType<AutoRecoveringConnection>(connection);
        }

        [Fact]
        public async Task Throws_when_no_endpoints_provided()
        {
            var endpoint = GetUniqueEndpoint();

            using var host = CreateOpenedContainerHost(endpoint);

            var exception = await Assert.ThrowsAsync<CreateConnectionException>(() => CreateConnection(Enumerable.Empty<Endpoint>()));
            Assert.Contains(exception.Message, "No endpoints provided.");
        }
    }
}