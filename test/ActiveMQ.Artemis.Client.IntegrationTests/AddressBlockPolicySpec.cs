using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace ActiveMQ.Artemis.Client.IntegrationTests
{
    public class AddressBlockPolicySpec : ActiveMQNetIntegrationSpec
    {
        public AddressBlockPolicySpec(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task Should_cancel_inflight_messages_when_send_cancelled()
        {
            await using var connection = await CreateConnection();
            var address = "address_full_policy";

            await BlockTheAddress(connection, address);

            var payload = CreatePayloadOfSize(900);
            await using var producer = await connection.CreateProducerAsync(address, RoutingType.Anycast);
            await Assert.ThrowsAsync<TaskCanceledException>(() => producer.SendAsync(new Message(payload), new CancellationTokenSource(TimeSpan.FromMilliseconds(100)).Token));

            await using var consumer = await connection.CreateConsumerAsync(address, RoutingType.Anycast);
            var messages = await GetAllMessages(connection, address);
            
            Assert.DoesNotContain(messages, message => payload.SequenceEqual(message.GetBody<byte[]>()));
        }

        private static async Task BlockTheAddress(IConnection connection, string address)
        {
            try
            {
                for (int i = 0; i < 1000; i++)
                {
                    await using var producer = await connection.CreateProducerAsync(address, RoutingType.Anycast);
                    var payload = CreatePayloadOfSize(1000);
                    await producer.SendAsync(new Message(payload), new CancellationTokenSource(TimeSpan.FromMilliseconds(100)).Token);
                    await Task.Delay(TimeSpan.FromMilliseconds(50));
                }
            }
            catch (Exception e)
            {
                Assert.IsType<TaskCanceledException>(e);
                return;
            }
            Assert.True(false, "Address hasn't been blocked.");
        }

        private static byte[] CreatePayloadOfSize(int size)
        {
            var bytes = new byte[size];
            new Random().NextBytes(bytes);
            return bytes;
        }

        private async Task<List<Message>> GetAllMessages(IConnection connection, string address)
        {
            await using var consumer = await connection.CreateConsumerAsync(address, RoutingType.Anycast);
            var messages = new List<Message>();
            try
            {
                while (true)
                {
                    var message = await consumer.ReceiveAsync(new CancellationTokenSource(TimeSpan.FromMilliseconds(100)).Token);
                    messages.Add(message);
                }
            }
            catch (OperationCanceledException)
            {
                // ignored
            }
            return messages;
        }
    }
}