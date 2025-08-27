using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace ActiveMQ.Artemis.Client.UnitTests
{
    public class MessagePrioritySpec : ActiveMQNetSpec
    {
        public MessagePrioritySpec(ITestOutputHelper output) : base(output)
        {
        }
        
        [Fact]
        public async Task Should_send_message_with_priority_not_set()
        {
            using var host = CreateOpenedContainerHost();
            var messageProcessor = host.CreateMessageProcessor("a1");
            await using var connection = await CreateConnection(host.Endpoint);
            await using var producer = await connection.CreateProducerAsync("a1", RoutingType.Anycast);

            var message = new Message("foo");
            await producer.SendAsync(message);

            var received = messageProcessor.Dequeue(Timeout);

            Assert.Null(received.Priority);
        }

        [Theory]
        [MemberData(nameof(ValidMessagePrioritiesData))]
        public async Task Should_send_message_with_valid_priority(byte? priority)
        {
            using var host = CreateOpenedContainerHost();
            var messageProcessor = host.CreateMessageProcessor("a1");
            await using var connection = await CreateConnection(host.Endpoint);
            await using var producer = await connection.CreateProducerAsync("a1", RoutingType.Anycast);

            var message = new Message("foo")
            {
                Priority = priority
            };
            await producer.SendAsync(message);

            var received = messageProcessor.Dequeue(Timeout);

            Assert.Equal(priority, received.Priority);
        }

        public static TheoryData<byte?> ValidMessagePrioritiesData => [null, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9];

        [Fact]
        public void Throws_when_invalid_priority_assigned()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var _ = new Message("foo")
                {
                    Priority = 10
                };
            });
        }

        [Fact]
        public async Task Should_take_message_priority_from_Producer_configuration()
        {
            using var host = CreateOpenedContainerHost();
            var messageProcessor = host.CreateMessageProcessor("a1");
            await using var connection = await CreateConnection(host.Endpoint);
            await using var producer = await connection.CreateProducerAsync(new ProducerConfiguration
            {
                Address = "a1",
                RoutingType = RoutingType.Anycast,
                MessagePriority = 9
            });

            await producer.SendAsync(new Message("foo"));

            var received = messageProcessor.Dequeue(Timeout);

            Assert.Equal((byte) 9, received.Priority);
        }
    }
}