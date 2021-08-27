using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace ActiveMQ.Artemis.Client.UnitTests
{
    public class MessageDurabilityModeSpec : ActiveMQNetSpec
    {
        public MessageDurabilityModeSpec(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task Should_SendAsync_message_with_Durable_durability_mode()
        {
            using var host = CreateOpenedContainerHost();
            var messageProcessor = host.CreateMessageProcessor("a1");
            await using var connection = await CreateConnection(host.Endpoint);
            await using var producer = await connection.CreateProducerAsync("a1", RoutingType.Anycast);

            await producer.SendAsync(new Message("foo"));

            var received = messageProcessor.Dequeue(Timeout);

            Assert.Equal(DurabilityMode.Durable, received.DurabilityMode);
        }

        [Fact]
        public async Task Should_Send_message_with_Nondurable_durability_mode()
        {
            using var host = CreateOpenedContainerHost();
            var messageProcessor = host.CreateMessageProcessor("a1");
            await using var connection = await CreateConnection(host.Endpoint);
            await using var producer = await connection.CreateProducerAsync("a1", RoutingType.Anycast);

            // ReSharper disable once MethodHasAsyncOverload
            producer.Send(new Message("foo"));

            var received = messageProcessor.Dequeue(Timeout);

            Assert.Equal(DurabilityMode.Nondurable, received.DurabilityMode);
        }

        [Fact]
        public async Task Should_SendAsync_message_with_Nondurable_durability_mode()
        {
            using var host = CreateOpenedContainerHost();
            var messageProcessor = host.CreateMessageProcessor("a1");
            await using var connection = await CreateConnection(host.Endpoint);
            await using var producer = await connection.CreateProducerAsync("a1", RoutingType.Anycast);

            await producer.SendAsync(new Message("foo") { DurabilityMode = DurabilityMode.Nondurable });

            var received = messageProcessor.Dequeue(Timeout);

            Assert.Equal(DurabilityMode.Nondurable, received.DurabilityMode);
        }

        [Fact]
        public async Task Should_Send_message_with_Durable_durability_mode()
        {
            using var host = CreateOpenedContainerHost();
            var messageProcessor = host.CreateMessageProcessor("a1");
            await using var connection = await CreateConnection(host.Endpoint);
            await using var producer = await connection.CreateProducerAsync("a1", RoutingType.Anycast);

            // ReSharper disable once MethodHasAsyncOverload
            producer.Send(new Message("foo") { DurabilityMode = DurabilityMode.Durable });

            var received = messageProcessor.Dequeue(Timeout);

            Assert.Equal(DurabilityMode.Durable, received.DurabilityMode);
        }

        [Fact]
        public async Task Should_override_durability_mode_with_producer_configuration_for_SendAsync()
        {
            using var host = CreateOpenedContainerHost();
            var messageProcessor = host.CreateMessageProcessor("a1");
            await using var connection = await CreateConnection(host.Endpoint);
            await using var producer = await connection.CreateProducerAsync(new ProducerConfiguration()
            {
                Address = "a1",
                RoutingType = RoutingType.Anycast,
                MessageDurabilityMode = DurabilityMode.Nondurable
            });

            await producer.SendAsync(new Message("foo"));

            var received = messageProcessor.Dequeue(Timeout);

            Assert.Equal(DurabilityMode.Nondurable, received.DurabilityMode);
        }

        [Fact]
        public async Task Should_override_durability_mode_with_producer_configuration_for_Send()
        {
            using var host = CreateOpenedContainerHost();
            var messageProcessor = host.CreateMessageProcessor("a1");
            await using var connection = await CreateConnection(host.Endpoint);
            await using var producer = await connection.CreateProducerAsync(new ProducerConfiguration
            {
                Address = "a1",
                RoutingType = RoutingType.Anycast,
                MessageDurabilityMode = DurabilityMode.Durable
            });

            // ReSharper disable once MethodHasAsyncOverload
            producer.Send(new Message("foo"));

            var received = messageProcessor.Dequeue(Timeout);

            Assert.Equal(DurabilityMode.Durable, received.DurabilityMode);
        }
    }
}