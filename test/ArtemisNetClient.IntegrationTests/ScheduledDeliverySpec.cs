﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace ActiveMQ.Artemis.Client.IntegrationTests
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
            var address = Guid.NewGuid().ToString();
            await using var producer = await connection.CreateProducerAsync(address, RoutingType.Anycast);
            await using var consumer = await connection.CreateConsumerAsync(address, RoutingType.Anycast);

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
            var address = Guid.NewGuid().ToString();
            await using var producer = await connection.CreateProducerAsync(address, RoutingType.Anycast);
            await using var consumer = await connection.CreateConsumerAsync(address, RoutingType.Anycast);

            await producer.SendAsync(new Message("foo")
            {
                ScheduledDeliveryDelay = TimeSpan.FromMilliseconds(400)
            });

            var msg = await consumer.ReceiveAsync();
            var delay = DateTime.UtcNow - msg.CreationTime;
            Assert.True(delay > msg.ScheduledDeliveryDelay);
        }

        [Fact]
        public async Task Should_prefer_ScheduledDeliveryTime_over_ScheduledDeliveryDelay_when_both_set()
        {
            await using var connection = await CreateConnection();
            var address = Guid.NewGuid().ToString();
            await using var producer = await connection.CreateProducerAsync(address, RoutingType.Anycast);
            await using var consumer = await connection.CreateConsumerAsync(address, RoutingType.Anycast);

            await producer.SendAsync(new Message("foo")
            {
                ScheduledDeliveryTime = DateTime.UtcNow.AddMilliseconds(800),
                ScheduledDeliveryDelay = TimeSpan.FromMilliseconds(400)
            });
            
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await consumer.ReceiveAsync(new CancellationTokenSource(TimeSpan.FromMilliseconds(500)).Token));

            var msg = await consumer.ReceiveAsync();
            Assert.Equal("foo", msg.GetBody<string>());
        }
    }
}