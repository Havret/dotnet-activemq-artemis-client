using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace ActiveMQ.Net.IntegrationTests
{
    public class ScheduledDeliverySpec : ActiveMQNetIntegrationSpec
    {
        public ScheduledDeliverySpec(ITestOutputHelper output) : base(output)
        {
        }
        
        [Fact]
        public async Task Should_deliver_message_at_scheduled_time()
        {
            await using var connection = await CreateConnection();
            var address = nameof(Should_deliver_message_at_scheduled_time);
            await using var producer = await connection.CreateProducerAsync(address, AddressRoutingType.Anycast);
            await using var consumer = await connection.CreateConsumerAsync(address, QueueRoutingType.Anycast);

            await producer.SendAsync(new Message("foo")
            {
                ScheduledDeliveryTime = DateTime.UtcNow.AddMilliseconds(500)
            });
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await consumer.ReceiveAsync(new CancellationTokenSource(TimeSpan.FromMilliseconds(250)).Token));

            var msg = await consumer.ReceiveAsync();
            Assert.Equal("foo", msg.GetBody<string>());
        }

        [Fact]
        public async Task Should_deliver_message_with_scheduled_delay()
        {
            await using var connection = await CreateConnection();
            var address = nameof(Should_deliver_message_with_scheduled_delay);
            await using var producer = await connection.CreateProducerAsync(address, AddressRoutingType.Anycast);
            await using var consumer = await connection.CreateConsumerAsync(address, QueueRoutingType.Anycast);

            await producer.SendAsync(new Message("foo")
            {
                ScheduledDeliveryDelay = TimeSpan.FromMilliseconds(150)
            });
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await consumer.ReceiveAsync(new CancellationTokenSource(TimeSpan.FromMilliseconds(100)).Token));

            var msg = await consumer.ReceiveAsync();
            Assert.Equal("foo", msg.GetBody<string>());
        }

        [Fact]
        public async Task Should_prefer_ScheduledDeliveryTime_over_ScheduledDeliveryDelay_when_both_set()
        {
            await using var connection = await CreateConnection();
            var address = nameof(Should_prefer_ScheduledDeliveryTime_over_ScheduledDeliveryDelay_when_both_set);
            await using var producer = await connection.CreateProducerAsync(address, AddressRoutingType.Anycast);
            await using var consumer = await connection.CreateConsumerAsync(address, QueueRoutingType.Anycast);

            await producer.SendAsync(new Message("foo")
            {
                ScheduledDeliveryTime = DateTime.UtcNow.AddMilliseconds(200),
                ScheduledDeliveryDelay = TimeSpan.FromMilliseconds(100)
            });
            
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await consumer.ReceiveAsync(new CancellationTokenSource(TimeSpan.FromMilliseconds(150)).Token));

            var msg = await consumer.ReceiveAsync();
            Assert.Equal("foo", msg.GetBody<string>());
        }
    }
}