﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace ActiveMQ.Artemis.Client.UnitTests
{
    public class ConsumerReceiveMessageSpec : ActiveMQNetSpec
    {
        public ConsumerReceiveMessageSpec(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task Should_receive_message()
        {
            var endpoint = GetUniqueEndpoint();
            using var host = CreateOpenedContainerHost(endpoint);

            var messageSource = host.CreateMessageSource("a1");
            await using var connection = await CreateConnection(endpoint);
            var consumer = await connection.CreateConsumerAsync("a1", RoutingType.Anycast);

            messageSource.Enqueue(new Message("foo"));
            var message = await consumer.ReceiveAsync();

            Assert.NotNull(message);
            Assert.Equal("foo", message.GetBody<string>());
        }

        [Fact]
        public async Task Should_be_able_to_cancel_ReceiveAsync_when_no_message_available()
        {
            var endpoint = GetUniqueEndpoint();
            using var host = CreateOpenedContainerHost(endpoint);

            host.CreateMessageSource("a1");
            await using var connection = await CreateConnection(endpoint);
            var consumer = await connection.CreateConsumerAsync("a1", RoutingType.Anycast);

            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromMilliseconds(50));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await consumer.ReceiveAsync(cts.Token));
        }
    }
}