using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace ActiveMQ.Artemis.Client.IntegrationTests
{
    public class ConsumerCreditSpec : ActiveMQNetIntegrationSpec
    {
        public ConsumerCreditSpec(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task Should_create_Consumer_with_custom_credit()
        {
            await using var connection = await CreateConnection();
            var address = nameof(Should_create_Consumer_with_custom_credit);
            await using var producer = await connection.CreateProducerAsync(address, RoutingType.Multicast);
            await using var consumer = await connection.CreateConsumerAsync(new ConsumerConfiguration
            {
                Address = address,
                RoutingType = RoutingType.Multicast,
                Credit = 2
            });

            await producer.SendAsync(new Message("foo1"));
            await producer.SendAsync(new Message("foo2"));
            await producer.SendAsync(new Message("foo3"));

            var msg1 = await consumer.ReceiveAsync();
            var msg2 = await consumer.ReceiveAsync();

            Assert.Equal("foo1", msg1.GetBody<string>());
            Assert.Equal("foo2", msg2.GetBody<string>());

            var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await consumer.ReceiveAsync(cancellationTokenSource.Token));
        }
    }
}