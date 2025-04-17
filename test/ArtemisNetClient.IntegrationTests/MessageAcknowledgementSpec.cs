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
        public async Task Should_redeliver_message_to_the_same_consumer_when_message_modified()
        {
            await using var connection = await CreateConnection();
            var address = Guid.NewGuid().ToString();
            await using var producer = await connection.CreateAnonymousProducerAsync();
            var consumer = await connection.CreateConsumerAsync(address, RoutingType.Anycast);

            await producer.SendAsync(address, RoutingType.Anycast, new Message("foo"));
            var msg = await consumer.ReceiveAsync();
            consumer.Modify(msg, deliveryFailed: true, undeliverableHere: false);

            msg = await consumer.ReceiveAsync(new CancellationTokenSource(TimeSpan.FromMilliseconds(500)).Token);
            Assert.Equal("foo", msg.GetBody<string>());
        }
        
        [Fact]
        public async Task Should_redeliver_message_to_different_consumer_when_message_modified_with_undeliverableHere_flag_enabled()
        {
            await using var connection = await CreateConnection();
            var address = Guid.NewGuid().ToString();
            await using var producer = await connection.CreateAnonymousProducerAsync();
            var consumer1 = await connection.CreateConsumerAsync(address, RoutingType.Anycast);

            await producer.SendAsync(address, RoutingType.Anycast, new Message("foo"));
            var msg = await consumer1.ReceiveAsync();
            consumer1.Modify(msg, deliveryFailed: true, undeliverableHere: true);
            
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await consumer1.ReceiveAsync(new CancellationTokenSource(TimeSpan.FromMilliseconds(100)).Token));
            
            var consumer2 = await connection.CreateConsumerAsync(address, RoutingType.Anycast);
            msg = await consumer2.ReceiveAsync(new CancellationTokenSource(TimeSpan.FromMilliseconds(500)).Token);
            Assert.Equal("foo", msg.GetBody<string>());
        }

        [Fact]
        public async Task Should_deliver_message_to_DLQ_when_message_rejected()
        {
            await using var connection = await CreateConnection();
            var address = Guid.NewGuid().ToString();
            await using var producer = await connection.CreateAnonymousProducerAsync();
            var consumer1 = await connection.CreateConsumerAsync(address, RoutingType.Anycast);

            string messageId = Guid.NewGuid().ToString();
            await producer.SendAsync(address, RoutingType.Anycast, new Message("foo") 
            {
                MessageId = messageId
            });
            var msg = await consumer1.ReceiveAsync();
            consumer1.Reject(msg);

            await consumer1.DisposeAsync();

            var consumer2 = await connection.CreateConsumerAsync("DLQ", RoutingType.Anycast);
            var rejectedMsg = await consumer2.ReceiveAsync();
            await consumer2.AcceptAsync(rejectedMsg);

            Assert.Equal(messageId, msg.MessageId);
            Assert.Equal(messageId, rejectedMsg.MessageId);
        }
    }
}