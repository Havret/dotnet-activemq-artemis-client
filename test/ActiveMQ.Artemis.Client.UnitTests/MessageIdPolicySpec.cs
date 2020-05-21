using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.MessageIdPolicy;
using Xunit;
using Xunit.Abstractions;

namespace ActiveMQ.Artemis.Client.UnitTests
{
    public class MessageIdPolicySpec : ActiveMQNetSpec
    {
        public MessageIdPolicySpec(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task Should_use_MessageIdPolicy_set_on_ConnectionFactory()
        {
            using var host = CreateOpenedContainerHost();
            var messageProcessor = host.CreateMessageProcessor("a1");
            var connectionFactory = CreateConnectionFactory();
            connectionFactory.MessageIdPolicyFactory = () => new TestMessageIdPolicy("ConnectionFactoryPolicyMessageId");
            
            await using var connection = await connectionFactory.CreateAsync(host.Endpoint);
            await using var producer = await connection.CreateProducerAsync("a1", AddressRoutingType.Anycast);

            var message = new Message("foo");
            await producer.SendAsync(message);

            var received = messageProcessor.Dequeue(Timeout);

            Assert.Equal("ConnectionFactoryPolicyMessageId", received.MessageId);
        }
        
        [Fact]
        public async Task Should_use_MessageIdPolicy_from_producer_configuration()
        {
            using var host = CreateOpenedContainerHost();
            var messageProcessor = host.CreateMessageProcessor("a1");
            var connectionFactory = CreateConnectionFactory();
            connectionFactory.MessageIdPolicyFactory = () => new TestMessageIdPolicy("ConnectionFactoryPolicyMessageId");
            
            await using var connection = await connectionFactory.CreateAsync(host.Endpoint);
            await using var producer = await connection.CreateProducerAsync(new ProducerConfiguration()
            {
                Address = "a1",
                RoutingType = AddressRoutingType.Anycast,
                MessageIdPolicy = new TestMessageIdPolicy("ProducerConfigurationPolicyMessageId")
            });

            var message = new Message("foo");
            await producer.SendAsync(message);

            var received = messageProcessor.Dequeue(Timeout);

            Assert.Equal("ProducerConfigurationPolicyMessageId", received.MessageId);
        }
        
        private class TestMessageIdPolicy : IMessageIdPolicy
        {
            private readonly string _messageId;
            public TestMessageIdPolicy(string messageId) => _messageId = messageId;
            public object GetNextMessageId() => _messageId;
        }
    }
}