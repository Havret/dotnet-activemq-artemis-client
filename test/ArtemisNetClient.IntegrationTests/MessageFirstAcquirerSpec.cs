using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace ActiveMQ.Artemis.Client.IntegrationTests
{
    public class MessageFirstAcquirerSpec : ActiveMQNetIntegrationSpec
    {
        public MessageFirstAcquirerSpec(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task Should_receive_msg_with_FirstAcquirer_set_when_msg_was_received_for_the_first_time()
        {
            await using var connection = await CreateConnection();
            var address = Guid.NewGuid().ToString();
            await using var producer = await connection.CreateProducerAsync(address, RoutingType.Anycast);
            await using var consumer1 = await connection.CreateConsumerAsync(address, RoutingType.Anycast);
            await using var consumer2 = await connection.CreateConsumerAsync(address, RoutingType.Anycast);

            await producer.SendAsync(new Message("foo"));
            var msg = await consumer1.ReceiveAsync(CancellationToken);
            Assert.True(msg.FirstAcquirer);
        }
        
        [Fact]
        public async Task Should_receive_msg_with_FirstAcquirer_set_to_false_when_msg_was_redelivered()
        {
            await using var connection = await CreateConnection();
            var address = Guid.NewGuid().ToString();
            await using var producer = await connection.CreateProducerAsync(address, RoutingType.Anycast);
            await using var consumer1 = await connection.CreateConsumerAsync(address, RoutingType.Anycast);
            await using var consumer2 = await connection.CreateConsumerAsync(address, RoutingType.Anycast);

            await producer.SendAsync(new Message("foo"));
            var msg = await consumer1.ReceiveAsync(CancellationToken);
            consumer1.Reject(msg);

            var msg2 = await consumer2.ReceiveAsync(CancellationToken);
            Assert.False(msg2.FirstAcquirer);
        }
    }
}