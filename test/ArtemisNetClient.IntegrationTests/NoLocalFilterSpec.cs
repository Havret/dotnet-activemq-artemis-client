using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace ActiveMQ.Artemis.Client.IntegrationTests
{
    public class NoLocalFilterSpec : ActiveMQNetIntegrationSpec
    {
        public NoLocalFilterSpec(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task Should_accept_only_messages_sent_using_different_connection()
        {
            var address = Guid.NewGuid().ToString();
            await using var connection1 = await CreateConnection();
            await using var producer1 = await connection1.CreateProducerAsync(address, RoutingType.Multicast);
            await using var consumer = await connection1.CreateConsumerAsync(new ConsumerConfiguration
            {
                Address = address,
                RoutingType = RoutingType.Multicast,
                NoLocalFilter = true
            });

            await using var connection2 = await CreateConnection();
            await using var producer2 = await connection2.CreateProducerAsync(address, RoutingType.Multicast);

            await producer1.SendAsync(new Message("connection1"));
            await producer2.SendAsync(new Message("connection2"));

            var msg = await consumer.ReceiveAsync(CancellationToken);
            Assert.Equal("connection2", msg.GetBody<string>());

            var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await consumer.ReceiveAsync(cts.Token));
        }
    }
}