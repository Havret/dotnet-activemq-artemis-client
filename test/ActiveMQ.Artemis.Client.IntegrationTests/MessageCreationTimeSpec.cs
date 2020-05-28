using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using ActiveMQ.Artemis.Client.InternalUtilities;

namespace ActiveMQ.Artemis.Client.IntegrationTests
{
    public class MessageCreationTimeSpec : ActiveMQNetIntegrationSpec
    {
        public MessageCreationTimeSpec(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task Should_be_set()
        {
            await using var connection = await CreateConnection();
            var address = nameof(Should_be_set);
            await using var producer = await connection.CreateAnonymousProducer();
            await using var consumer = await connection.CreateConsumerAsync(address, RoutingType.Anycast);

            var creationTime = DateTime.UtcNow.DropTicsPrecision();
            await producer.SendAsync(address, RoutingType.Anycast, new Message("foo") { CreationTime = creationTime });
            var msg = await consumer.ReceiveAsync();

            Assert.Equal(creationTime, msg.CreationTime);
        }

        [Fact]
        public async Task Should_not_be_set()
        {
            await using var connection = await CreateConnection();
            var address = nameof(Should_not_be_set);
            await using var producer = await connection.CreateAnonymousProducer(new AnonymousProducerConfiguration { SetMessageCreationTime = false });
            await using var consumer = await connection.CreateConsumerAsync(address, RoutingType.Anycast);

            await producer.SendAsync(address, RoutingType.Anycast, new Message("foo"));
            var msg = await consumer.ReceiveAsync();

            Assert.Null(msg.CreationTime);
        }
    }
}