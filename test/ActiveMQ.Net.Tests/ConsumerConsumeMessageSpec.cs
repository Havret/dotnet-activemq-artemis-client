using System.Threading.Tasks;
using ActiveMQ.Net.Tests.Utils;
using Xunit;

namespace ActiveMQ.Net.Tests
{
    public class ConsumerConsumeMessageSpec
    {
        [Fact]
        public async Task Should_consume_message()
        {
            var address = AddressUtil.GetAddress();
            using var host = new TestContainerHost(address);
            host.Open();
            
            var messageSource = host.CreateMessageSource("a1");
            await using var connection = await CreateConnection(address);
            var consumer = await connection.CreateConsumerAsync("a1");
            
            messageSource.Enqueue(new Message("foo"));
            var message = await consumer.ConsumeAsync();
            
            Assert.NotNull(message);
            Assert.Equal("foo", message.GetBody<string>());
        }

        private static Task<IConnection> CreateConnection(string address)
        {
            var connectionFactory = new ConnectionFactory();
            return connectionFactory.CreateAsync(address);
        }
    }
}