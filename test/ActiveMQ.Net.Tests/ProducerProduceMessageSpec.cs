using System;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Net.Tests.Utils;
using Amqp.Handler;
using Xunit;

namespace ActiveMQ.Net.Tests
{
    public class ProducerProduceMessageSpec : ActiveMQNetSpec
    {
        [Fact]
        public async Task Should_send_msg_and_wait_for_confirmation_from_the_server()
        {
            var address = GetUniqueAddress();
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

            using var host = CreateOpenedContainerHost(address, testHandler);

            await using var connection = await CreateConnection(address);
            var producer = connection.CreateProducer("a1");

            await producer.ProduceAsync(new Message("foo"));

            Assert.True(deliveryReceived.WaitOne(TimeSpan.FromSeconds(10)));
            Assert.False(deliverySettled);
        }

        [Fact]
        public async Task Should_send_msg_with_Settled_delivery_frame_when_used_in_fire_and_forget_manner()
        {
            var address = GetUniqueAddress();
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

            using var host = CreateOpenedContainerHost(address, testHandler);

            await using var connection = await CreateConnection(address);
            var producer = connection.CreateProducer("a1");

            producer.Produce(new Message("foo"));

            Assert.True(deliveryReceived.WaitOne(TimeSpan.FromSeconds(10)));
            Assert.True(deliverySettled);
        }
    }
}