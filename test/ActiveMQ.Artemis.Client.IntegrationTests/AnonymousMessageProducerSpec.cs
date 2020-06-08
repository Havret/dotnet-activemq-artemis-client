using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace ActiveMQ.Artemis.Client.IntegrationTests
{
    public class AnonymousMessageProducerSpec : ActiveMQNetIntegrationSpec
    {
        public AnonymousMessageProducerSpec(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task Should_send_message_to_specified_address_using_AnycastRoutingType()
        {
            await using var connection = await CreateConnection();
            var address = nameof(Should_send_message_to_specified_address_using_AnycastRoutingType);
            await using var producer = await connection.CreateAnonymousProducerAsync();
            await using var consumer = await connection.CreateConsumerAsync(address, RoutingType.Anycast);

            await producer.SendAsync(address, RoutingType.Anycast, new Message("foo"));
            var msg = await consumer.ReceiveAsync();

            Assert.Equal("foo", msg.GetBody<string>());
        }

        [Fact]
        public async Task Should_send_message_to_specified_address_using_MulticastRoutingType()
        {
            await using var connection = await CreateConnection();
            var address = nameof(Should_send_message_to_specified_address_using_MulticastRoutingType);
            await using var producer = await connection.CreateAnonymousProducerAsync();
            await using var consumer = await connection.CreateConsumerAsync(address, RoutingType.Multicast);

            await producer.SendAsync(address, RoutingType.Multicast, new Message("foo"));
            var msg = await consumer.ReceiveAsync();

            Assert.Equal("foo", msg.GetBody<string>());
        }
    }
}