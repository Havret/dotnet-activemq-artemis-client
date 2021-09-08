﻿using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace ActiveMQ.Artemis.Client.IntegrationTests
{
    public class MessageExpirySpec : ActiveMQNetIntegrationSpec
    {
        public MessageExpirySpec(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task Should_deliver_expired_message_to_expiry_queue()
        {
            await using var connection = await CreateConnection();
            var address = Guid.NewGuid().ToString();
            await using var producer = await connection.CreateProducerAsync(address, RoutingType.Anycast);
            await using var expiryQueueConsumer = await connection.CreateConsumerAsync("ExpiryQueue", RoutingType.Anycast);

            await producer.SendAsync(new Message("foo") { TimeToLive = TimeSpan.FromMilliseconds(100) });
            var msg = await expiryQueueConsumer.ReceiveAsync();

            Assert.Equal("foo", msg.GetBody<string>());
        }
    }
}