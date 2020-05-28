using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.UnitTests.Utils;
using Amqp.Framing;
using Amqp.Handler;
using Amqp.Types;
using Xunit;
using Xunit.Abstractions;

namespace ActiveMQ.Artemis.Client.UnitTests
{
    public class ProducerSpec : ActiveMQNetSpec
    {
        public ProducerSpec(ITestOutputHelper output) : base(output)
        {
        }
        
        [Fact]
        public async Task Should_be_created_and_closed()
        {
            var endpoint = GetUniqueEndpoint();
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

            using var host = CreateOpenedContainerHost(endpoint, testHandler);

            await using var connection = await CreateConnection(endpoint);
            var producer = await connection.CreateProducerAsync("a1", RoutingType.Anycast);
            await producer.DisposeAsync();

            Assert.True(producerAttached.WaitOne(Timeout));
            Assert.True(producerClosed.WaitOne(Timeout));
        }

        [Fact]
        public async Task Should_attach_to_specified_address()
        {
            var endpoint = GetUniqueEndpoint();
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

            using var host = CreateOpenedContainerHost(endpoint, testHandler);

            await using var connection = await CreateConnection(endpoint);
            await using var producer = await connection.CreateProducerAsync("a1", RoutingType.Anycast);

            Assert.True(producerAttached.WaitOne(Timeout));
            Assert.IsType<Target>(attachFrame.Target);
            Assert.Equal("a1", ((Target) attachFrame.Target).Address);
        }

        [Fact]
        public async Task Should_attach_to_anycast_address_when_no_RoutingType_specified()
        {
            var endpoint = GetUniqueEndpoint();
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

            using var host = CreateOpenedContainerHost(endpoint, testHandler);

            await using var connection = await CreateConnection(endpoint);
            await using var producer = await connection.CreateProducerAsync("a1", RoutingType.Anycast);

            Assert.True(producerAttached.WaitOne(Timeout));
            Assert.NotNull(attachFrame);
            Assert.IsType<Target>(attachFrame.Target);
            Assert.Contains(((Target) attachFrame.Target).Capabilities, symbol => RoutingCapabilities.Anycast.Equals(symbol));
        }
        
        [Theory, MemberData(nameof(RoutingTypesData))]
        public async Task Should_attach_to_address_with_specified_RoutingType(RoutingType routingType, Symbol routingCapability)
        {
            var endpoint = GetUniqueEndpoint();
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

            using var host = CreateOpenedContainerHost(endpoint, testHandler);

            await using var connection = await CreateConnection(endpoint);
            await using var consumer = await connection.CreateProducerAsync("a1", routingType);

            Assert.True(producerAttached.WaitOne(Timeout));
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
            var endpoint = GetUniqueEndpoint();
            using var host = CreateOpenedContainerHost(endpoint);

            await using var connection = await CreateConnection(endpoint);
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => connection.CreateProducerAsync("a1", (RoutingType) 99));
        }

        [Fact]
        public async Task Should_cancel_CreateProducerAsync_when_address_and_routing_type_specified_but_attach_frame_not_received_on_time()
        {
            var endpoint = GetUniqueEndpoint();
            using var host = CreateContainerHostThatWillNeverSendAttachFrameBack(endpoint);
            await using var connection = await CreateConnection(endpoint);

            var cancellationTokenSource = new CancellationTokenSource(ShortTimeout);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => connection.CreateProducerAsync("a1", RoutingType.Multicast, cancellationTokenSource.Token));
        }
        
        [Fact]
        public async Task Should_cancel_CreateProducerAsync_when_address_specified_but_attach_frame_not_received_on_time()
        {
            var endpoint = GetUniqueEndpoint();
            using var host = CreateContainerHostThatWillNeverSendAttachFrameBack(endpoint);
            await using var connection = await CreateConnection(endpoint);

            var cancellationTokenSource = new CancellationTokenSource(ShortTimeout);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => connection.CreateProducerAsync("a1", RoutingType.Multicast, cancellationTokenSource.Token));
        }
        
        [Fact]
        public async Task Throws_when_created_with_null_configuration()
        {
            var endpoint = GetUniqueEndpoint();
            using var host = CreateOpenedContainerHost(endpoint);
            await using var connection = await CreateConnection(endpoint);

            await Assert.ThrowsAsync<ArgumentNullException>(() => connection.CreateProducerAsync(null));
        }
    }
}