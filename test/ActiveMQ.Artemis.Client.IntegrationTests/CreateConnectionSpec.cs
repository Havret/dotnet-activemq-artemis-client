using System;
using System.Reflection;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Exceptions;
using Xunit;
using Xunit.Abstractions;

namespace ActiveMQ.Artemis.Client.IntegrationTests
{
    public class CreateConnectionSpec : ActiveMQNetIntegrationSpec
    {
        public CreateConnectionSpec(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task Should_throw_security_exception_when_wrong_credentials_provided_and_connection_auto_recovery_enabled()
        {
            var defaultEndpoint = GetEndpoint();
            var endpoint = Endpoint.Create(
                password: "wrongPassword",
                user: defaultEndpoint.User,
                host: defaultEndpoint.Host,
                port: defaultEndpoint.Port,
                scheme: defaultEndpoint.Scheme
            );

            var connectionFactory = new ConnectionFactory
            {
                AutomaticRecoveryEnabled = true
            };

            var createConnectionException = await Assert.ThrowsAnyAsync<CreateConnectionException>(() => connectionFactory.CreateAsync(endpoint));
            Assert.Equal("amqp:unauthorized-access", createConnectionException.ErrorCode);
        }
    }
}