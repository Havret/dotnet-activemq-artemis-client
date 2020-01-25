using System;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Net.Tests.Utils;
using Amqp.Handler;
using Xunit;
using Xunit.Abstractions;

namespace ActiveMQ.Net.Tests
{
    public class ProducerSendMessageSpec : ActiveMQNetSpec
    {
        public ProducerSendMessageSpec(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task Should_send_msg_and_wait_for_confirmation_from_the_server()
        {
            var endpoint = GetUniqueEndpoint();
            var deliveryReceived = new ManualResetEvent(false);
            var deliverySettled = false;

            var testHandler = new TestHandler(@event =>
            {
                switch (@event.Id)
                {
                    case EventId.ReceiveDelivery when @event.Context is IDelivery delivery:
                        deliverySettled = delivery.Settled;
                        deliveryReceived.Set();
                        break;
                }
            });

            using var host = CreateOpenedContainerHost(endpoint, testHandler);

            await using var connection = await CreateConnection(endpoint);
            var producer = await connection.CreateProducerAsync("a1");

            await producer.SendAsync(new Message("foo"));

            Assert.True(deliveryReceived.WaitOne(Timeout));
            Assert.False(deliverySettled);
        }

        [Fact]
        public async Task Should_send_msg_with_Settled_delivery_frame_when_used_in_fire_and_forget_manner()
        {
            var endpoint = GetUniqueEndpoint();
            var deliveryReceived = new ManualResetEvent(false);
            var deliverySettled = false;

            var testHandler = new TestHandler(@event =>
            {
                switch (@event.Id)
                {
                    case EventId.ReceiveDelivery when @event.Context is IDelivery delivery:
                        deliverySettled = delivery.Settled;
                        deliveryReceived.Set();
                        break;
                }
            });

            using var host = CreateOpenedContainerHost(endpoint, testHandler);

            await using var connection = await CreateConnection(endpoint);
            var producer = await connection.CreateProducerAsync("a1");

            producer.Send(new Message("foo"));

            Assert.True(deliveryReceived.WaitOne(Timeout));
            Assert.True(deliverySettled);
        }

        [Fact]
        public async Task Should_be_able_to_cancel_SendAsync_when_no_outcome_from_remote_peer_available()
        {
            var endpoint = GetUniqueEndpoint();
            var testHandler = new TestHandler(@event =>
            {
                switch (@event.Id)
                {
                    case EventId.ReceiveDelivery:
                        // postpone sending outcome from a remote peer
                        Task.Delay(TimeSpan.FromMilliseconds(500)).GetAwaiter().GetResult();
                        break;
                }
            });

            using var host = CreateOpenedContainerHost(endpoint, testHandler);

            await using var connection = await CreateConnection(endpoint);
            var producer = await connection.CreateProducerAsync("a1");
            
            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromMilliseconds(50));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await producer.SendAsync(new Message("foo"), cts.Token));
        }
    }
}