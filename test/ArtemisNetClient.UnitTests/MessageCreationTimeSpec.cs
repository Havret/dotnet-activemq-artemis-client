using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using ActiveMQ.Artemis.Client.InternalUtilities;

namespace ActiveMQ.Artemis.Client.UnitTests
{
    public class MessageCreationTimeSpec : ActiveMQNetSpec
    {
        public MessageCreationTimeSpec(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task Should_be_set_by_producer()
        {
            using var host = CreateOpenedContainerHost();
            var messageProcessor = host.CreateMessageProcessor("a1");
            await using var connection = await CreateConnection(host.Endpoint);
            await using var producer = await connection.CreateProducerAsync("a1", RoutingType.Anycast);

            var before = DateTime.UtcNow.DropTicsPrecision();
            await producer.SendAsync(new Message("foo"));
            var after = DateTime.UtcNow.DropTicsPrecision();

            var received = messageProcessor.Dequeue(Timeout);

            Assert.NotNull(received.CreationTime);
            Assert.True(received.CreationTime.Value >= before, "CreationTime should be after 'before' timestamp");
            Assert.True(received.CreationTime.Value <= after, "CreationTime should be before 'after' timestamp");
        }

        [Fact]
        public async Task Should_not_be_set_by_producer_when_functionality_disabled()
        {
            using var host = CreateOpenedContainerHost();
            var messageProcessor = host.CreateMessageProcessor("a1");
            await using var connection = await CreateConnection(host.Endpoint);
            await using var producer = await connection.CreateProducerAsync(new ProducerConfiguration
            {
                Address = "a1",
                RoutingType = RoutingType.Anycast,
                SetMessageCreationTime = false
            });

            await producer.SendAsync(new Message("foo"));

            var received = messageProcessor.Dequeue(Timeout);
            
            Assert.Null(received.CreationTime);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Should_be_set_explicitly_on_message(bool setMessageCreationTimeFlag)
        {
            using var host = CreateOpenedContainerHost();
            var messageProcessor = host.CreateMessageProcessor("a1");
            await using var connection = await CreateConnection(host.Endpoint);
            await using var producer = await connection.CreateProducerAsync(new ProducerConfiguration
            {
                Address = "a1",
                RoutingType = RoutingType.Anycast,
                SetMessageCreationTime = setMessageCreationTimeFlag
            });

            var creationTime = new DateTime(2020, 5, 2).ToUniversalTime();
            await producer.SendAsync(new Message("foo") { CreationTime = creationTime });

            var received = messageProcessor.Dequeue(Timeout);
            
            Assert.Equal(creationTime, received.CreationTime);
        }
    }
}