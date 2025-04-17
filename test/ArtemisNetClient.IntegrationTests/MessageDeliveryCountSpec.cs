using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace ActiveMQ.Artemis.Client.IntegrationTests
{
    public class MessageDeliveryCountSpec : ActiveMQNetIntegrationSpec
    {
        public MessageDeliveryCountSpec(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task Should_receive_msg_with_DeliveryCount_0_when_msg_was_received_for_the_first_time()
        {
            await using var connection = await CreateConnection();
            var address = Guid.NewGuid().ToString();
            await using var producer = await connection.CreateProducerAsync(address, RoutingType.Anycast);
            await using var consumer1 = await connection.CreateConsumerAsync(address, RoutingType.Anycast);
            await using var consumer2 = await connection.CreateConsumerAsync(address, RoutingType.Anycast);

            await producer.SendAsync(new Message("foo"));
            var msg = await consumer1.ReceiveAsync(CancellationToken);
            Assert.Equal(0u, msg.DeliveryCount);
        }

        [Fact]
        public async Task Should_receive_msg_with_DeliveryCount_set_to_1_when_msg_was_redelivered()
        {
            await using var connection = await CreateConnection();
            var address = Guid.NewGuid().ToString();
            await using var producer = await connection.CreateProducerAsync(address, RoutingType.Anycast);
            await using var consumer1 = await connection.CreateConsumerAsync(address, RoutingType.Anycast);
            await using var consumer2 = await connection.CreateConsumerAsync(address, RoutingType.Anycast);

            await producer.SendAsync(new Message("foo"));
            var msg = await consumer1.ReceiveAsync(CancellationToken);
            consumer1.Modify(msg, deliveryFailed: true, undeliverableHere: true);

            var msg2 = await consumer2.ReceiveAsync(CancellationToken);
            Assert.Equal(1u, msg2.DeliveryCount);
        }

        [Fact]
        public async Task Should_receive_msg_with_DeliveryCount_set_to_0_when_msg_was_modified_with_deliveryFailed_set_to_false()
        {
            await using var connection = await CreateConnection();
            var address = Guid.NewGuid().ToString();
            await using var producer = await connection.CreateProducerAsync(address, RoutingType.Anycast);
            await using var consumer1 = await connection.CreateConsumerAsync(address, RoutingType.Anycast);
            await using var consumer2 = await connection.CreateConsumerAsync(address, RoutingType.Anycast);

            await producer.SendAsync(new Message("foo"));
            var msg = await consumer1.ReceiveAsync(CancellationToken);
            consumer1.Modify(msg, deliveryFailed: false, undeliverableHere: true);

            var msg2 = await consumer2.ReceiveAsync(CancellationToken);
            Assert.Equal(0u, msg2.DeliveryCount);
        }
    }
}