using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Net.Tests.Utils;
using Amqp.Framing;
using Amqp.Handler;
using Amqp.Types;
using Xunit;

namespace ActiveMQ.Net.Tests
{
    public class ConsumerSpec
    {
        [Fact]
        public async Task Should_be_created_and_closed()
        {
            var address = AddressUtil.GetAddress();
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

            using var host = new TestContainerHost(address, testHandler);
            host.Open();

            await using var connection = await CreateConnection(address);
            var consumer = connection.CreateConsumer("test-consumer");
            await consumer.DisposeAsync();

            Assert.True(consumerAttached.WaitOne(TimeSpan.FromSeconds(10)));
            Assert.True(consumerClosed.WaitOne(TimeSpan.FromSeconds(10)));
        }

        [Fact]
        public async Task Should_attach_to_specified_address()
        {
            var address = AddressUtil.GetAddress();
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

            using var host = new TestContainerHost(address, testHandler);
            host.Open();

            await using var connection = await CreateConnection(address);
            await using var consumer = connection.CreateConsumer("test-consumer");

            Assert.True(consumerAttached.WaitOne(TimeSpan.FromSeconds(10)));
            Assert.IsType<Source>(attachFrame.Source);
            Assert.Equal("test-consumer", ((Source) attachFrame.Source).Address);
        }

        [Fact]
        public async Task Should_attach_to_anycast_address_when_no_RoutingType_specified()
        {
            var address = AddressUtil.GetAddress();
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

            using var host = new TestContainerHost(address, testHandler);
            host.Open();

            await using var connection = await CreateConnection(address);
            await using var consumer = connection.CreateConsumer("test-consumer");

            Assert.True(consumerAttached.WaitOne(TimeSpan.FromSeconds(10)));
            Assert.NotNull(attachFrame);
            Assert.IsType<Source>(attachFrame.Source);
            Assert.Contains(((Source) attachFrame.Source).Capabilities, symbol => RoutingCapabilities.Anycast.Equals(symbol));
        }

        [Theory, MemberData(nameof(RoutingTypesData))]
        public async Task Should_attach_to_address_with_specified_RoutingType(RoutingType routingType, Symbol routingCapability)
        {
            var address = AddressUtil.GetAddress();
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

            using var host = new TestContainerHost(address, testHandler);
            host.Open();

            await using var connection = await CreateConnection(address);
            await using var consumer = connection.CreateConsumer("test-consumer", routingType);

            Assert.True(consumerAttached.WaitOne(TimeSpan.FromSeconds(10)));
            Assert.NotNull(attachFrame);
            Assert.IsType<Source>(attachFrame.Source);
            Assert.Contains(((Source) attachFrame.Source).Capabilities, routingCapability.Equals);
        }

        public static IEnumerable<object[]> RoutingTypesData()
        {
            return new[]
            {
                new object[] { RoutingType.Anycast, RoutingCapabilities.Anycast },
                new object[] { RoutingType.Multicast, RoutingCapabilities.Multicast }
            };
        }

        [Fact]
        public async Task Throws_when_created_with_invalid_RoutingType()
        {
            var address = AddressUtil.GetAddress();
            using var host = new TestContainerHost(address);
            host.Open();

            await using var connection = await CreateConnection(address);
            Assert.Throws<ArgumentOutOfRangeException>(() => connection.CreateConsumer("test-consumer", (RoutingType) 99));
        }

        [Fact]
        public async Task Should_connect_to_a_custom_queue_on_specified_address_with_an_anycast_routing_type()
        {
            var address = AddressUtil.GetAddress();
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

            using var host = new TestContainerHost(address, testHandler);
            host.Open();

            await using var connection = await CreateConnection(address);
            await using var consumer = connection.CreateConsumer("test-consumer", RoutingType.Anycast, "q1");

            Assert.True(consumerAttached.WaitOne(TimeSpan.FromSeconds(10)));
            Assert.NotNull(attachFrame);
            Assert.IsType<Source>(attachFrame.Source);
            var sourceFrame = (Source) attachFrame.Source;
            Assert.Equal("test-consumer::q1", sourceFrame.Address);
            Assert.Contains(sourceFrame.Capabilities, RoutingCapabilities.Anycast.Equals);
        }

        private static Task<IConnection> CreateConnection(string address)
        {
            var connectionFactory = new ConnectionFactory();
            return connectionFactory.CreateAsync(address);
        }
    }
}