using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace ActiveMQ.Artemis.Client.IntegrationTests
{
    public class MessageAcknowledgementSpec : ActiveMQNetIntegrationSpec
    {
        public MessageAcknowledgementSpec(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task Should_acknowledge_message()
        {
            await using var connection = await CreateConnection();
            var address = Guid.NewGuid().ToString();
            await using var producer = await connection.CreateAnonymousProducerAsync();
            var consumer1 = await connection.CreateConsumerAsync(address, RoutingType.Anycast);

            await producer.SendAsync(address, RoutingType.Anycast, new Message("foo"));
            var msg = await consumer1.ReceiveAsync();
            await consumer1.AcceptAsync(msg);

            await consumer1.DisposeAsync();

            var consumer2 = await connection.CreateConsumerAsync(address, RoutingType.Anycast);

            // As message was accepted by consumer1, it shouldn't be redeliver to newly created consumer2
            // after consumer1 was closed
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await consumer2.ReceiveAsync(new CancellationTokenSource(TimeSpan.FromMilliseconds(100)).Token));
        }

        [Fact]
        public async Task Should_redeliver_message_to_the_same_consumer_when_message_rejected()
        {
            await using var connection = await CreateConnection();
            var address = Guid.NewGuid().ToString();
            await using var producer = await connection.CreateAnonymousProducerAsync();
            var consumer = await connection.CreateConsumerAsync(address, RoutingType.Anycast);

            await producer.SendAsync(address, RoutingType.Anycast, new Message("foo"));
            var msg = await consumer.ReceiveAsync();
            consumer.Reject(msg);

            msg = await consumer.ReceiveAsync(new CancellationTokenSource(TimeSpan.FromMilliseconds(500)).Token);
            Assert.Equal("foo", msg.GetBody<string>());
        }
        
        [Fact]
        public async Task Should_redeliver_message_to_different_consumer_when_message_rejected_with_undeliverableHere_flag_enabled()
        {
            await using var connection = await CreateConnection();
            var address = Guid.NewGuid().ToString();
            await using var producer = await connection.CreateAnonymousProducerAsync();
            var consumer1 = await connection.CreateConsumerAsync(address, RoutingType.Anycast);

            await producer.SendAsync(address, RoutingType.Anycast, new Message("foo"));
            var msg = await consumer1.ReceiveAsync();
            consumer1.Reject(msg, undeliverableHere: true);
            
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await consumer1.ReceiveAsync(new CancellationTokenSource(TimeSpan.FromMilliseconds(100)).Token));
            
            var consumer2 = await connection.CreateConsumerAsync(address, RoutingType.Anycast);
            msg = await consumer2.ReceiveAsync(new CancellationTokenSource(TimeSpan.FromMilliseconds(500)).Token);
            Assert.Equal("foo", msg.GetBody<string>());
        }
    }
}