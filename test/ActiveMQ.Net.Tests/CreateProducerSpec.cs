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
    public class ProducerSpec : ActiveMQNetSpec
    {
        [Fact]
        public async Task Should_be_created_and_closed()
        {
            var address = GetUniqueAddress();
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

            using var host = CreateOpenedContainerHost(address, testHandler);

            await using var connection = await CreateConnection(address);
            var producer = connection.CreateProducer("a1");
            await producer.DisposeAsync();

            Assert.True(producerAttached.WaitOne(TimeSpan.FromSeconds(10)));
            Assert.True(producerClosed.WaitOne(TimeSpan.FromSeconds(10)));
        }

        [Fact]
        public async Task Should_attach_to_specified_address()
        {
            var address = GetUniqueAddress();
            var producerAttached = new ManualResetEvent(false);
            Attach attachFrame = null;

            var testHandler = new TestHandler(@event =>
            {
                switch (@event.Id)
                {
                    case EventId.LinkRemoteOpen when @event.Context is Attach attach:
                        attachFrame = attach;
                        producerAttached.Set();
                        break;
                }
            });

            using var host = CreateOpenedContainerHost(address, testHandler);

            await using var connection = await CreateConnection(address);
            await using var producer = connection.CreateProducer("a1");

            Assert.True(producerAttached.WaitOne(TimeSpan.FromSeconds(10)));
            Assert.IsType<Target>(attachFrame.Target);
            Assert.Equal("a1", ((Target) attachFrame.Target).Address);
        }

        [Fact]
        public async Task Should_attach_to_anycast_address_when_no_RoutingType_specified()
        {
            var address = GetUniqueAddress();
            var producerAttached = new ManualResetEvent(false);
            Attach attachFrame = null;

            var testHandler = new TestHandler(@event =>
            {
                switch (@event.Id)
                {
                    case EventId.LinkRemoteOpen when @event.Context is Attach attach:
                        attachFrame = attach;
                        producerAttached.Set();
                        break;
                }
            });

            using var host = CreateOpenedContainerHost(address, testHandler);

            await using var connection = await CreateConnection(address);
            await using var producer = connection.CreateProducer("a1");

            Assert.True(producerAttached.WaitOne(TimeSpan.FromSeconds(10)));
            Assert.NotNull(attachFrame);
            Assert.IsType<Target>(attachFrame.Target);
            Assert.Contains(((Target) attachFrame.Target).Capabilities, symbol => RoutingCapabilities.Anycast.Equals(symbol));
        }
        
        [Theory, MemberData(nameof(RoutingTypesData))]
        public async Task Should_attach_to_address_with_specified_RoutingType(RoutingType routingType, Symbol routingCapability)
        {
            var address = GetUniqueAddress();
            var producerAttached = new ManualResetEvent(false);
            Attach attachFrame = null;

            var testHandler = new TestHandler(@event =>
            {
                switch (@event.Id)
                {
                    case EventId.LinkRemoteOpen when @event.Context is Attach attach:
                        attachFrame = attach;
                        producerAttached.Set();
                        break;
                }
            });

            using var host = CreateOpenedContainerHost(address, testHandler);

            await using var connection = await CreateConnection(address);
            await using var consumer = connection.CreateProducer("a1", routingType);

            Assert.True(producerAttached.WaitOne(TimeSpan.FromSeconds(10)));
            Assert.NotNull(attachFrame);
            Assert.IsType<Target>(attachFrame.Target);
            Assert.Contains(((Target) attachFrame.Target).Capabilities, routingCapability.Equals);
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
            var address = AddressUtil.GetUniqueAddress();
            using var host = CreateOpenedContainerHost(address);

            await using var connection = await CreateConnection(address);
            Assert.Throws<ArgumentOutOfRangeException>(() => connection.CreateProducer("a1", (RoutingType) 99));
        }
    }
}