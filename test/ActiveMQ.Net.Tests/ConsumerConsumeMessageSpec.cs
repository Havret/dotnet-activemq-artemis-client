using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace ActiveMQ.Net.Tests
{
    public class ConsumerConsumeMessageSpec : ActiveMQNetSpec
    {
        public ConsumerConsumeMessageSpec(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task Should_receive_message()
        {
            var address = GetUniqueAddress();
            using var host = CreateOpenedContainerHost(address);

            var messageSource = host.CreateMessageSource("a1");
            await using var connection = await CreateConnection(address);
            var consumer = await connection.CreateConsumerAsync("a1");

            messageSource.Enqueue(new Message("foo"));
            var message = await consumer.ReceiveAsync();

            Assert.NotNull(message);
            Assert.Equal("foo", message.GetBody<string>());
        }

        [Fact]
        public async Task Should_be_able_to_cancel_ReceiveAsync_when_no_message_available()
        {
            var address = GetUniqueAddress();
            using var host = CreateOpenedContainerHost(address);

            host.CreateMessageSource("a1");
            await using var connection = await CreateConnection(address);
            var consumer = await connection.CreateConsumerAsync("a1");

            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromMilliseconds(50));
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await consumer.ReceiveAsync(cts.Token));
        }
    }
}