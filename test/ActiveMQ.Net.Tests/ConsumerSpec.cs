using System;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Net.Tests.Utils;
using Amqp.Framing;
using Amqp.Handler;
using Xunit;

namespace ActiveMQ.Net.Tests
{
    public class ConsumerSpec
    {
        private readonly string _address = "amqp://guest:guest@localhost:15672";

        [Fact]
        public async Task Should_create_and_close_consumer()
        {
            var consumerAttached = new ManualResetEvent(false);
            var consumerClosed = new ManualResetEvent(false);

            var testHandler = new TestHandler(@event =>
            {
                switch (@event.Id)
                {
                    case EventId.LinkRemoteOpen when @event.Context is Attach attach && !attach.Role:
                        consumerAttached.Set();
                        break;
                    case EventId.LinkRemoteClose when @event.Context is Detach detach && detach.Closed:
                        consumerClosed.Set();
                        break;
                }
            });

            using var host = new TestContainerHost(_address, testHandler);
            host.Open();

            await using var connection = await CreateConnection(_address);
            var consumer = connection.CreateConsumer("test-consumer");
            await consumer.DisposeAsync();

            Assert.True(consumerAttached.WaitOne(TimeSpan.FromSeconds(10)));
            Assert.True(consumerClosed.WaitOne(TimeSpan.FromSeconds(10)));
        }

        [Fact]
        public async Task Consumer_should_attach_to_specified_address()
        {
            var consumerAttached = new ManualResetEvent(false);
            Attach attachFrame = null;

            var testHandler = new TestHandler(@event =>
            {
                switch (@event.Id)
                {
                    case EventId.LinkRemoteOpen when @event.Context is Attach attach:
                        attachFrame = attach;
                        consumerAttached.Set();
                        break;
                }
            });

            using var host = new TestContainerHost(_address, testHandler);
            host.Open();

            await using var connection = await CreateConnection(_address);
            await using var consumer = connection.CreateConsumer("test-consumer");

            Assert.True(consumerAttached.WaitOne(TimeSpan.FromSeconds(10)));
            Assert.IsType<Source>(attachFrame.Source);
            Assert.Equal("test-consumer", ((Source) attachFrame.Source).Address);
        }

        [Fact]
        public async Task Consumer_should_attach_to_anycast_address_when_no_routing_type_specified()
        {
            var consumerAttached = new ManualResetEvent(false);
            Attach attachFrame = null;

            var testHandler = new TestHandler(@event =>
            {
                switch (@event.Id)
                {
                    case EventId.LinkRemoteOpen when @event.Context is Attach attach:
                        attachFrame = attach;
                        consumerAttached.Set();
                        break;
                }
            });

            using var host = new TestContainerHost(_address, testHandler);
            host.Open();

            await using var connection = await CreateConnection(_address);
            await using var consumer = connection.CreateConsumer("test-consumer");

            Assert.True(consumerAttached.WaitOne(TimeSpan.FromSeconds(10)));
            Assert.NotNull(attachFrame);
            Assert.IsType<Source>(attachFrame.Source);
            Assert.Contains(((Source) attachFrame.Source).Capabilities, symbol => RoutingCapabilities.Anycast.Equals(symbol));
        }

        private static Task<IConnection> CreateConnection(string address)
        {
            var connectionFactory = new ConnectionFactory();
            return connectionFactory.CreateAsync(address);
        }
    }
}