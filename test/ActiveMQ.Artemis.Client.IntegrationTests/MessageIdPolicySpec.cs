using System;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.MessageIdPolicy;
using Xunit;
using Xunit.Abstractions;

namespace ActiveMQ.Artemis.Client.IntegrationTests
{
    public class MessageIdPolicySpec : ActiveMQNetIntegrationSpec
    {
        public MessageIdPolicySpec(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task Should_not_have_message_id_set_when_DisableMessageIdPolicy_used()
        {
            var address = Guid.NewGuid().ToString();
            await using var connection = await CreateConnection();
            await using var producer = await connection.CreateProducerAsync(new ProducerConfiguration
            {
                Address = address,
                RoutingType = AddressRoutingType.Anycast,
                MessageIdPolicy = MessageIdPolicyFactory.DisableMessageIdPolicy()
            });
            await using var consumer = await connection.CreateConsumerAsync(address, QueueRoutingType.Anycast);

            await producer.SendAsync(new Message("foo"));

            var msg = await consumer.ReceiveAsync(CancellationToken);

            Assert.Null(msg.MessageId);
        }

        [Fact]
        public async Task Should_have_guid_message_id_when_GuidMessageIdPolicy_used()
        {
            var address = Guid.NewGuid().ToString();
            await using var connection = await CreateConnection();
            await using var producer = await connection.CreateProducerAsync(new ProducerConfiguration
            {
                Address = address,
                RoutingType = AddressRoutingType.Anycast,
                MessageIdPolicy = MessageIdPolicyFactory.GuidMessageIdPolicy()
            });
            await using var consumer = await connection.CreateConsumerAsync(address, QueueRoutingType.Anycast);

            await producer.SendAsync(new Message("foo"));

            var msg = await consumer.ReceiveAsync(CancellationToken);

            Assert.NotEqual(default, msg.GetMessageId<Guid>());
        }

        [Fact]
        public async Task Should_have_string_guid_message_id_when_StringGuidMessageIdPolicy_used()
        {
            var address = Guid.NewGuid().ToString();
            await using var connection = await CreateConnection();
            await using var producer = await connection.CreateProducerAsync(new ProducerConfiguration
            {
                Address = address,
                RoutingType = AddressRoutingType.Anycast,
                MessageIdPolicy = MessageIdPolicyFactory.StringGuidMessageIdPolicy()
            });
            await using var consumer = await connection.CreateConsumerAsync(address, QueueRoutingType.Anycast);

            await producer.SendAsync(new Message("foo"));

            var msg = await consumer.ReceiveAsync(CancellationToken);

            Assert.NotNull(msg.MessageId);
            Assert.True(Guid.TryParse(msg.MessageId, out _));
        }
        
        [Fact]
        public async Task Should_ignore_message_id_policy_when_message_id_explicitly_set_on_message()
        {
            var address = Guid.NewGuid().ToString();
            await using var connection = await CreateConnection();
            await using var producer = await connection.CreateProducerAsync(new ProducerConfiguration
            {
                Address = address,
                RoutingType = AddressRoutingType.Anycast,
                MessageIdPolicy = MessageIdPolicyFactory.StringGuidMessageIdPolicy()
            });
            await using var consumer = await connection.CreateConsumerAsync(address, QueueRoutingType.Anycast);

            var message = new Message("foo");
            message.SetMessageId(ulong.MaxValue);
            await producer.SendAsync(message);

            var received = await consumer.ReceiveAsync(CancellationToken);

            Assert.Equal(ulong.MaxValue, received.GetMessageId<ulong>());
        }
    }
}