using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace ActiveMQ.Artemis.Client.IntegrationTests
{
    public class MessageGroupingSpec : ActiveMQNetIntegrationSpec
    {
        public MessageGroupingSpec(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task Should_deliver_messages_with_the_same_GroupId_to_the_same_consumer()
        {
            var address = nameof(Should_deliver_messages_with_the_same_GroupId_to_the_same_consumer);
            await using var connection = await CreateConnection();
            await using var producer = await connection.CreateProducerAsync(address, RoutingType.Anycast);

            await using var consumer1 = await connection.CreateConsumerAsync(address, RoutingType.Anycast);
            await using var consumer2 = await connection.CreateConsumerAsync(address, RoutingType.Anycast);
            await using var consumer3 = await connection.CreateConsumerAsync(address, RoutingType.Anycast);

            await SendMessagesToGroup(producer, "group1", 5);
            await SendMessagesToGroup(producer, "group2", 5);
            await SendMessagesToGroup(producer, "group3", 5);

            await AssertReceivedAllMessagesWithTheSameGroupId(consumer1, 5);
            await AssertReceivedAllMessagesWithTheSameGroupId(consumer2, 5);
            await AssertReceivedAllMessagesWithTheSameGroupId(consumer3, 5);
        }

        private async Task SendMessagesToGroup(IProducer producer, string groupId, int count)
        {
            for (int i = 1; i <= count; i++)
            {
                await producer.SendAsync(new Message(groupId)
                {
                    GroupId = groupId,
                     GroupSequence = (uint) i
                });
            }
        }

        private async Task AssertReceivedAllMessagesWithTheSameGroupId(IConsumer consumer, int count)
        {
            var messages = new List<Message>();
            for (int i = 1; i <= count; i++)
            {
                var message = await consumer.ReceiveAsync();
                Assert.Equal((uint) i, message.GroupSequence);
                messages.Add(message);
            }

            Assert.Single(messages.GroupBy(x => x.GroupId));
        }
    }
}