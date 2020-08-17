using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace ActiveMQ.Artemis.Client.IntegrationTests
{
    public class MessageToSpec : ActiveMQNetIntegrationSpec
    {
        public MessageToSpec(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task Should_receive_message_with_the_address_it_was_destined_for()
        {
            var toAddress = Guid.NewGuid().ToString();

            await using var connection = await CreateConnection();
            await using var producer = await connection.CreateProducerAsync(toAddress, RoutingType.Anycast);
            await using var consumer = await connection.CreateConsumerAsync(toAddress, RoutingType.Anycast);

            await producer.SendAsync(new Message("foo"), CancellationToken);

            Assert.Equal(toAddress, (await consumer.ReceiveAsync(CancellationToken)).To);
        }
    }
}