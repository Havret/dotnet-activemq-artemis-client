using System.Threading.Tasks;
using Xunit;

namespace ActiveMQ.Net.Tests
{
    public class ConsumerConsumeMessageSpec : ActiveMQNetSpec
    {
        [Fact]
        public async Task Should_consume_message()
        {
            var address = GetUniqueAddress();
            using var host = CreateOpenedContainerHost(address);

            var messageSource = host.CreateMessageSource("a1");
            await using var connection = await CreateConnection(address);
            var consumer = await connection.CreateConsumerAsync("a1");

            messageSource.Enqueue(new Message("foo"));
            var message = await consumer.ConsumeAsync();

            Assert.NotNull(message);
            Assert.Equal("foo", message.GetBody<string>());
        }
    }
}