using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace ActiveMQ.Artemis.Client.IntegrationTests
{
    public class MessageReplyToSpec : ActiveMQNetIntegrationSpec
    {
        public MessageReplyToSpec(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task Should_send_message_with_the_address_of_the_node_to_send_replied_to()
        {
            var toAddress = Guid.NewGuid().ToString();
            var replyToAddress = Guid.NewGuid().ToString();

            await using var connection = await CreateConnection();
            await using var producer = await connection.CreateProducerAsync(toAddress, RoutingType.Anycast);
            await using var replyRecipient = await connection.CreateConsumerAsync(replyToAddress, RoutingType.Anycast);

            await producer.SendAsync(new Message("request") { ReplyTo = replyToAddress }, CancellationToken);

            await using var consumer = await connection.CreateConsumerAsync(toAddress, RoutingType.Anycast);
            var requestMsg = await consumer.ReceiveAsync(CancellationToken);

            await using var anonymousProducer = await connection.CreateAnonymousProducerAsync();
            await anonymousProducer.SendAsync(requestMsg.ReplyTo, RoutingType.Anycast, new Message("reply"), CancellationToken);

            Assert.Equal("reply", (await replyRecipient.ReceiveAsync()).GetBody<string>());
        }
    }
}