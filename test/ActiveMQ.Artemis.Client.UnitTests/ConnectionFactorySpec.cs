using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.AutoRecovering;
using ActiveMQ.Artemis.Client.AutoRecovering.RecoveryPolicy;
using ActiveMQ.Artemis.Client.Exceptions;
using ActiveMQ.Artemis.Client.UnitTests.Utils;
using Amqp.Handler;
using Xunit;
using Xunit.Abstractions;

namespace ActiveMQ.Artemis.Client.UnitTests
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
            Assert.Contains((string) exception.Message, "No endpoints provided.");
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Should_cancel_CreateAsync_when_no_open_frame_received_within_specified_timeout(bool automaticRecoveryEnabled)
        {
            var endpoint = GetUniqueEndpoint();

            var timeout = new ManualResetEvent(false);
            var handler = new TestHandler(@event =>
            {
                switch (@event.Id)
                {
                    case EventId.ConnectionLocalOpen:
                        timeout.WaitOne(Timeout);
                        break;
                }
            });
            using var host = CreateOpenedContainerHost(endpoint, handler);

            var connectionFactory = CreateConnectionFactory();
            connectionFactory.AutomaticRecoveryEnabled = automaticRecoveryEnabled;

            var cts = new CancellationTokenSource(ShortTimeout);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => connectionFactory.CreateAsync(endpoint, cts.Token));
            timeout.Set();
        }

        [Fact]
        public void Should_throw_exception_when_null_assigned_as_recovery_policy()
        {
            var connectionFactory = CreateConnectionFactory();
            Assert.Throws<ArgumentNullException>(() => { connectionFactory.RecoveryPolicy = null; });
        }

        [Fact]
        public void Should_be_possible_to_assign_custom_recovery_policy()
        {
            var connectionFactory = CreateConnectionFactory();
            connectionFactory.RecoveryPolicy = RecoveryPolicyFactory.ConstantBackoff(TimeSpan.FromSeconds(1));
        }
    }
}