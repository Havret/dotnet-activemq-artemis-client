using System;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Net.Tests.Utils;
using Amqp.Framing;
using Amqp.Handler;
using Xunit;

namespace ActiveMQ.Net.Tests
{
    public class ProducerSpec
    {
        [Fact]
        public async Task Should_be_created_and_closed()
        {
            var address = AddressUtil.GetAddress();
            var producerAttached = new ManualResetEvent(false);
            var producerClosed = new ManualResetEvent(false);

            var testHandler = new TestHandler(@event =>
            {
                switch (@event.Id)
                {
                    case EventId.LinkRemoteOpen when @event.Context is Attach attach && attach.Role:
                        producerAttached.Set();
                        break;
                    case EventId.LinkRemoteClose when @event.Context is Detach detach && detach.Closed:
                        producerClosed.Set();
                        break;
                }
            });

            using var host = new TestContainerHost(address, testHandler);
            host.Open();

            await using var connection = await CreateConnection(address);
            var producer = connection.CreateProducer("a1");
            await producer.DisposeAsync();

            Assert.True(producerAttached.WaitOne(TimeSpan.FromSeconds(10)));
            Assert.True(producerClosed.WaitOne(TimeSpan.FromSeconds(10)));
        }
        
        private static Task<IConnection> CreateConnection(string address)
        {
            var connectionFactory = new ConnectionFactory();
            return connectionFactory.CreateAsync(address);
        }
    }
}