using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace ActiveMQ.Artemis.Client.IntegrationTests
{
    public class MessageSubjectSpec : ActiveMQNetIntegrationSpec
    {
        public MessageSubjectSpec(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task Should_send_message_with_subject_specified()
        {
            var address = Guid.NewGuid().ToString();
            await using var connection = await CreateConnection();
            await using var producer = await connection.CreateProducerAsync(address, RoutingType.Anycast);

            await producer.SendAsync(new Message("foo") { Subject = "MyCustomSubject" });

            await using var consumer = await connection.CreateConsumerAsync(address, RoutingType.Anycast);

            Assert.Equal("MyCustomSubject", (await consumer.ReceiveAsync(CancellationToken)).Subject);
        }
    }
}