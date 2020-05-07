using System.Threading.Tasks;
using Amqp.Framing;
using Xunit;
using Xunit.Abstractions;

namespace ActiveMQ.Artemis.Client.UnitTests
{
    public class ConsumerMessageAcknowledgementSpec : ActiveMQNetSpec
    {
        public ConsumerMessageAcknowledgementSpec(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task Should_not_send_any_disposition_frames_until_message_is_accepted()
        {
            var endpoint = GetUniqueEndpoint();
            using var host = CreateOpenedContainerHost(endpoint);

            var messageSource = host.CreateMessageSource("a1");
            await using var connection = await CreateConnection(endpoint);
            var consumer = await connection.CreateConsumerAsync("a1", QueueRoutingType.Anycast);
            
            messageSource.Enqueue(new Message("foo"));
            var message = await consumer.ReceiveAsync();

            Assert.NotNull(message);
            var dispositionContext = messageSource.GetNextDisposition(ShortTimeout);
            Assert.Null(dispositionContext);
        }
        
        [Fact]
        public async Task Should_send_accepted_and_settled_disposition_frame_when_message_accepted()
        {
            var endpoint = GetUniqueEndpoint();
            using var host = CreateOpenedContainerHost(endpoint);

            var messageSource = host.CreateMessageSource("a1");
            await using var connection = await CreateConnection(endpoint);
            var consumer = await connection.CreateConsumerAsync("a1", QueueRoutingType.Anycast);
            
            messageSource.Enqueue(new Message("foo"));
            var message = await consumer.ReceiveAsync();
            await consumer.AcceptAsync(message);

            var dispositionContext = messageSource.GetNextDisposition(Timeout);
            Assert.IsType<Accepted>(dispositionContext.DeliveryState);
            Assert.True(dispositionContext.Settled);
        }

        [Fact]
        public async Task Should_send_rejected_disposition_frame_when_message_rejected()
        {
            var endpoint = GetUniqueEndpoint();
            using var host = CreateOpenedContainerHost(endpoint);

            var messageSource = host.CreateMessageSource("a1");
            await using var connection = await CreateConnection(endpoint);
            var consumer = await connection.CreateConsumerAsync("a1", QueueRoutingType.Anycast);
            
            messageSource.Enqueue(new Message("foo"));
            var message = await consumer.ReceiveAsync();
            consumer.Reject(message);

            var dispositionContext = messageSource.GetNextDisposition(Timeout);
            Assert.IsType<Rejected>(dispositionContext.DeliveryState);
            Assert.True(dispositionContext.Settled);
        }
    }
}