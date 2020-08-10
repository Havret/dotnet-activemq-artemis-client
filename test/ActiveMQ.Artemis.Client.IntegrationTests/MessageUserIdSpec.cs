using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace ActiveMQ.Artemis.Client.IntegrationTests
{
    public class MessageUserIdSpec : ActiveMQNetIntegrationSpec
    {
        public MessageUserIdSpec(ITestOutputHelper output) : base(output)
        {
        }
        
        [Fact]
        public async Task Should_send_message_with_user_id_specified()
        {
            var address = Guid.NewGuid().ToString();
            await using var connection = await CreateConnection();
            await using var producer = await connection.CreateProducerAsync(address, RoutingType.Anycast);

            await producer.SendAsync(new Message("foo") { UserId = new byte[] { 1, 2, 4 } });

            await using var consumer = await connection.CreateConsumerAsync(address, RoutingType.Anycast);

            Assert.Equal(new byte[] { 1, 2, 4 } , (await consumer.ReceiveAsync(CancellationToken)).UserId);
        }
    }
}