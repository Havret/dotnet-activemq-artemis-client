using System.Threading.Tasks;
using ActiveMQ.Net.AutoRecovering;
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
            var address = GetUniqueAddress();

            using var host = CreateOpenedContainerHost(address);

            var connectionFactory = new ConnectionFactory { LoggerFactory = CreateTestLoggerFactory(), AutomaticRecoveryEnabled = false };
            await using var connection = await connectionFactory.CreateAsync(address);
            Assert.IsType<Connection>(connection);
        }

        [Fact]
        public async Task Should_create_auto_recovering_connection()
        {
            var address = GetUniqueAddress();

            using var host = CreateOpenedContainerHost(address);

            var connectionFactory = new ConnectionFactory { LoggerFactory = CreateTestLoggerFactory(), AutomaticRecoveryEnabled = true };
            await using var connection = await connectionFactory.CreateAsync(address);
            Assert.IsType<AutoRecoveringConnection>(connection);
        }
    }
}