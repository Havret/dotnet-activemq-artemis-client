using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace ActiveMQ.Net.IntegrationTests
{
    public class AnonymousMessageProducerSpec : ActiveMQNetIntegrationSpec
    {
        public AnonymousMessageProducerSpec(ITestOutputHelper output) : base(output)
        {
        }
        
        [Fact]
        public async Task Should_send_message_to_specified_address_using_AnycastRoutingType()
        {
            var connection = await CreateConnection();
            var address = nameof(Should_send_message_to_specified_address_using_AnycastRoutingType);
            var producer = await connection.CreateAnonymousProducer();
            var consumer = await connection.CreateConsumerAsync(address, QueueRoutingType.Anycast);

            await producer.SendAsync(address, AddressRoutingType.Anycast, new Message("foo"));
            var msg = await consumer.ReceiveAsync();
            
            Assert.Equal("foo", msg.GetBody<string>());
        }
        
        [Fact]
        public async Task Should_send_message_to_specified_address_using_MulticastRoutingType()
        {
            var connection = await CreateConnection();
            var address = nameof(Should_send_message_to_specified_address_using_MulticastRoutingType);
            var producer = await connection.CreateAnonymousProducer();
            var consumer = await connection.CreateConsumerAsync(address, QueueRoutingType.Multicast);

            await producer.SendAsync(address, AddressRoutingType.Multicast, new Message("foo"));
            var msg = await consumer.ReceiveAsync();
            
            Assert.Equal("foo", msg.GetBody<string>());
        }
    }
}