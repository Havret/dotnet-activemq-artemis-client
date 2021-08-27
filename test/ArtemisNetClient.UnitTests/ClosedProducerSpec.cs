using System;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Exceptions;
using Xunit;
using Xunit.Abstractions;

namespace ActiveMQ.Artemis.Client.UnitTests
{
    public class ClosedProducerSpec : ActiveMQNetSpec
    {
        public ClosedProducerSpec(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task Throws_on_attempt_to_Send_message_using_disposed_producer()
        {
            using var host = CreateOpenedContainerHost();

            await using var connection = await CreateConnection(host.Endpoint);
            var producer = await connection.CreateProducerAsync("a1", RoutingType.Anycast);

            await producer.DisposeAsync();

            Assert.Throws<ObjectDisposedException>(() => producer.Send(new Message("foo"), CancellationToken));
        }
        
        [Fact]
        public async Task Throws_on_attempt_to_Send_message_when_connection_disconnected()
        {
            using var host = CreateOpenedContainerHost();

            var connection = await CreateConnectionWithoutAutomaticRecovery(host.Endpoint);
            await using var producer = await connection.CreateProducerAsync("a1", RoutingType.Anycast);

            await DisposeHostAndWaitUntilConnectionNotified(host, connection);

            Assert.Throws<ProducerClosedException>(() => producer.Send(new Message("foo"), CancellationToken));
        }
        
        [Fact]
        public async Task Throws_on_attempt_to_Send_message_when_connection_disposed()
        {
            using var host = CreateOpenedContainerHost();
        
            await using var connection = await CreateConnection(host.Endpoint);
            await using var producer = await connection.CreateProducerAsync("a1", RoutingType.Anycast);

            await connection.DisposeAsync();
        
            Assert.Throws<ObjectDisposedException>(() => producer.Send(new Message("foo"), CancellationToken));
        }
        
        [Fact]
        public async Task Throws_on_attempt_to_SendAsync_message_using_disposed_producer()
        {
            using var host = CreateOpenedContainerHost();

            await using var connection = await CreateConnection(host.Endpoint);
            var producer = await connection.CreateProducerAsync("a1", RoutingType.Anycast);

            await producer.DisposeAsync();

            await Assert.ThrowsAsync<ObjectDisposedException>(() => producer.SendAsync(new Message("foo"), CancellationToken));
        }
        
        [Fact]
        public async Task Throws_on_attempt_to_SendAsync_message_when_connection_disconnected()
        {
            using var host = CreateOpenedContainerHost();

            var connection = await CreateConnectionWithoutAutomaticRecovery(host.Endpoint);
            await using var producer = await connection.CreateProducerAsync("a1", RoutingType.Anycast);

            await DisposeHostAndWaitUntilConnectionNotified(host, connection);

            await Assert.ThrowsAsync<ProducerClosedException>(() => producer.SendAsync(new Message("foo"), CancellationToken));
        }
        
        [Fact]
        public async Task Throws_on_attempt_to_SendAsync_message_when_connection_disposed()
        {
            using var host = CreateOpenedContainerHost();

            await using var connection = await CreateConnection(host.Endpoint);
            await using var producer = await connection.CreateProducerAsync("a1", RoutingType.Anycast);

            await connection.DisposeAsync();

            await Assert.ThrowsAsync<ObjectDisposedException>(() => producer.SendAsync(new Message("foo"), CancellationToken));
        }

        [Fact]
        public async Task Should_dispose_the_same_producer_multiple_times()
        {
            using var host = CreateOpenedContainerHost();

            await using var connection = await CreateConnection(host.Endpoint);
            var producer = await connection.CreateProducerAsync("a1", RoutingType.Anycast);

            await producer.DisposeAsync();
            await producer.DisposeAsync();
            await producer.DisposeAsync();
        }
    }
}