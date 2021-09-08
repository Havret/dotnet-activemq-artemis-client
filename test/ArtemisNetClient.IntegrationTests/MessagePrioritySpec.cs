﻿using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace ActiveMQ.Artemis.Client.IntegrationTests
{
    public class MessagePrioritySpec : ActiveMQNetIntegrationSpec
    {
        public MessagePrioritySpec(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task Should_receive_messages_ordered_via_priority()
        {
            var address = Guid.NewGuid().ToString();
            await using var connection = await CreateConnection();
            await using var producer = await connection.CreateProducerAsync(address, RoutingType.Anycast);

            for (var i = 0; i <= 9; i++)
            {
                await producer.SendAsync(new Message(i) { Priority = (byte) i });
            }

            await using var consumer = await connection.CreateConsumerAsync(address, RoutingType.Anycast);

            for (var i = 9; i >= 0; i--)
            {
                var message = await consumer.ReceiveAsync();
                Assert.Equal((byte) i, message.Priority);
                Assert.Equal(i, message.GetBody<int>());
            }
        }

        [Fact]
        public async Task Messages_without_priority_should_be_delivered_with_priority_4()
        {
            var address = Guid.NewGuid().ToString();
            await using var connection = await CreateConnection();
            await using var producer = await connection.CreateProducerAsync(address, RoutingType.Anycast);

            await producer.SendAsync(new Message("low_priority") { Priority = 0 });
            await producer.SendAsync(new Message("normal_priority") { Priority = 4 });
            await producer.SendAsync(new Message("no_priority"));
            await producer.SendAsync(new Message("normal_priority") { Priority = 4 });
            await producer.SendAsync(new Message("high_priority") { Priority = 9 });

            await using var consumer = await connection.CreateConsumerAsync(address, RoutingType.Anycast);

            Assert.Equal("high_priority", (await consumer.ReceiveAsync()).GetBody<string>());
            Assert.Equal("normal_priority", (await consumer.ReceiveAsync()).GetBody<string>());
            Assert.Equal("no_priority", (await consumer.ReceiveAsync()).GetBody<string>());
            Assert.Equal("normal_priority", (await consumer.ReceiveAsync()).GetBody<string>());
            Assert.Equal("low_priority", (await consumer.ReceiveAsync()).GetBody<string>());
        }

        [Fact]
        public async Task Should_take_message_priority_from_producer_configuration()
        {
            var address = Guid.NewGuid().ToString();
            await using var connection = await CreateConnection();
            await using var producer = await connection.CreateAnonymousProducerAsync(new AnonymousProducerConfiguration { MessagePriority = 9 });
            await using var consumer = await connection.CreateConsumerAsync(address, RoutingType.Anycast);

            await producer.SendAsync(address, RoutingType.Anycast, new Message("foo"));

            Assert.Equal((byte) 9, (await consumer.ReceiveAsync()).Priority);
        }
    }
}