using System;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Net.Tests.Utils;
using Amqp.Handler;
using Xunit;

namespace ActiveMQ.Net.Tests
{
    public class ProducerProduceMessageSpec
    {
        [Fact]
        public async Task Should_send_msg_and_wait_for_confirmation_from_the_server()
        {
            var address = AddressUtil.GetAddress();
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

            using var host = new TestContainerHost(address, testHandler);
            host.Open();

            await using var connection = await CreateConnection(address);
            var producer = connection.CreateProducer("a1");

            await producer.ProduceAsync(new Message("foo"));

            Assert.True(deliveryReceived.WaitOne(TimeSpan.FromSeconds(10)));
            Assert.False(deliverySettled);
        }

        private static Task<IConnection> CreateConnection(string address)
        {
            var connectionFactory = new ConnectionFactory();
            return connectionFactory.CreateAsync(address);
        }
    }
}