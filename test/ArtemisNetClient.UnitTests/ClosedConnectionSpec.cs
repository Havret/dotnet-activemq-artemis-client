using System;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Exceptions;
using Xunit;
using Xunit.Abstractions;

namespace ActiveMQ.Artemis.Client.UnitTests
{
    public class ClosedConnectionSpec : ActiveMQNetSpec
    {
        public ClosedConnectionSpec(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task Throws_on_attempt_to_create_producer_using_disposed_connection()
        {
            using var host = CreateOpenedContainerHost();

            var connection = await CreateConnection(host.Endpoint);
            await connection.DisposeAsync();

            await Assert.ThrowsAsync<ObjectDisposedException>(() => connection.CreateProducerAsync("a1", RoutingType.Anycast));
        }

        [Fact]
        public async Task Throws_on_attempt_to_create_anonymous_producer_using_disposed_connection()
        {
            using var host = CreateOpenedContainerHost();

            var connection = await CreateConnection(host.Endpoint);
            await connection.DisposeAsync();

            await Assert.ThrowsAsync<ObjectDisposedException>(() => connection.CreateAnonymousProducerAsync());
        }

        [Fact]
        public async Task Throws_on_attempt_to_create_consumer_using_disposed_connection()
        {
            using var host = CreateOpenedContainerHost();

            var connection = await CreateConnection(host.Endpoint);
            await connection.DisposeAsync();

            await Assert.ThrowsAsync<ObjectDisposedException>(() => connection.CreateConsumerAsync("a1", RoutingType.Anycast));
        }

        [Fact]
        public async Task Throws_on_attempt_to_create_topology_manager_using_disposed_connection()
        {
            using var host = CreateOpenedContainerHost();

            var connection = await CreateConnection(host.Endpoint);
            await connection.DisposeAsync();

            await Assert.ThrowsAsync<ObjectDisposedException>(() => connection.CreateTopologyManagerAsync());
        }

        [Fact]
        public async Task Throws_on_attempt_to_create_producer_using_closed_connection()
        {
            using var host = CreateOpenedContainerHost();
            var connection = await CreateConnectionWithoutAutomaticRecovery(host.Endpoint);
            await DisposeHostAndWaitUntilConnectionNotified(host, connection);

            await Assert.ThrowsAsync<ConnectionClosedException>(() => connection.CreateProducerAsync("a1", RoutingType.Anycast));
        }

        [Fact]
        public async Task Throws_on_attempt_to_create_anonymous_producer_using_closed_connection()
        {
            using var host = CreateOpenedContainerHost();
            var connection = await CreateConnectionWithoutAutomaticRecovery(host.Endpoint);
            await DisposeHostAndWaitUntilConnectionNotified(host, connection);

            await Assert.ThrowsAsync<ConnectionClosedException>(() => connection.CreateAnonymousProducerAsync());
        }

        [Fact]
        public async Task Throws_on_attempt_to_create_consumer_using_closed_connection()
        {
            using var host = CreateOpenedContainerHost();
            var connection = await CreateConnectionWithoutAutomaticRecovery(host.Endpoint);
            await DisposeHostAndWaitUntilConnectionNotified(host, connection);

            await Assert.ThrowsAsync<ConnectionClosedException>(() => connection.CreateConsumerAsync("a1", RoutingType.Anycast));
        }

        [Fact]
        public async Task Throws_on_attempt_to_create_topology_manager_using_closed_connection()
        {
            using var host = CreateOpenedContainerHost();
            var connection = await CreateConnectionWithoutAutomaticRecovery(host.Endpoint);
            await DisposeHostAndWaitUntilConnectionNotified(host, connection);

            await Assert.ThrowsAsync<ConnectionClosedException>(() => connection.CreateTopologyManagerAsync());
        }
    }
}