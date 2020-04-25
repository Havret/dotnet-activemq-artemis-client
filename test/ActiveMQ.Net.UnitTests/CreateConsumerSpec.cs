using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Net.Exceptions;
using ActiveMQ.Net.Tests.Utils;
using Amqp;
using Amqp.Framing;
using Amqp.Handler;
using Amqp.Types;
using Xunit;
using Xunit.Abstractions;

namespace ActiveMQ.Net.Tests
{
    public class CreateConsumerSpec : ActiveMQNetSpec
    {
        public CreateConsumerSpec(ITestOutputHelper output) : base(output)
        {
        }
        
        [Fact]
        public async Task Should_be_created_and_closed()
        {
            var endpoint = GetUniqueEndpoint();
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

            using var host = CreateOpenedContainerHost(endpoint, testHandler);
            await using var connection = await CreateConnection(endpoint);
            var consumer = await connection.CreateConsumerAsync("test-consumer", QueueRoutingType.Anycast);
            await consumer.DisposeAsync();

            Assert.True(consumerAttached.WaitOne(Timeout));
            Assert.True(consumerClosed.WaitOne(Timeout));
        }

        [Fact]
        public async Task Should_attach_to_specified_address()
        {
            var endpoint = GetUniqueEndpoint();
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

            using var host = CreateOpenedContainerHost(endpoint, testHandler);

            await using var connection = await CreateConnection(endpoint);
            await using var consumer = await connection.CreateConsumerAsync("test-consumer", QueueRoutingType.Anycast);

            Assert.True(consumerAttached.WaitOne(Timeout));
            Assert.IsType<Source>(attachFrame.Source);
            Assert.Equal("test-consumer", ((Source) attachFrame.Source).Address);
        }

        [Fact]
        public async Task Should_attach_to_anycast_address_when_no_RoutingType_specified()
        {
            var endpoint = GetUniqueEndpoint();
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

            using var host = CreateOpenedContainerHost(endpoint, testHandler);

            await using var connection = await CreateConnection(endpoint);
            await using var consumer = await connection.CreateConsumerAsync("test-consumer", QueueRoutingType.Anycast);

            Assert.True(consumerAttached.WaitOne(Timeout));
            Assert.NotNull(attachFrame);
            Assert.IsType<Source>(attachFrame.Source);
            Assert.Contains(((Source) attachFrame.Source).Capabilities, symbol => RoutingCapabilities.Anycast.Equals(symbol));
        }

        [Theory, MemberData(nameof(RoutingTypesData))]
        public async Task Should_attach_to_address_with_specified_RoutingType(QueueRoutingType routingType, Symbol routingCapability)
        {
            var endpoint = GetUniqueEndpoint();
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

            using var host = CreateOpenedContainerHost(endpoint, testHandler);

            await using var connection = await CreateConnection(endpoint);
            await using var consumer = await connection.CreateConsumerAsync("test-consumer", routingType);

            Assert.True(consumerAttached.WaitOne(Timeout));
            Assert.NotNull(attachFrame);
            Assert.IsType<Source>(attachFrame.Source);
            Assert.Contains(((Source) attachFrame.Source).Capabilities, routingCapability.Equals);
        }

        public static IEnumerable<object[]> RoutingTypesData()
        {
            return new[]
            {
                new object[] { QueueRoutingType.Anycast, RoutingCapabilities.Anycast },
                new object[] { QueueRoutingType.Multicast, RoutingCapabilities.Multicast }
            };
        }

        [Fact]
        public async Task Throws_when_created_with_invalid_RoutingType()
        {
            var endpoint = GetUniqueEndpoint();
            using var host = CreateOpenedContainerHost(endpoint);

            await using var connection = await CreateConnection(endpoint);
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => connection.CreateConsumerAsync("test-consumer", (QueueRoutingType) 99));
        }

        [Fact]
        public async Task Should_connect_to_a_custom_queue_on_specified_address_with_an_anycast_routing_type()
        {
            var endpoint = GetUniqueEndpoint();
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

            using var host = CreateOpenedContainerHost(endpoint, testHandler);

            await using var connection = await CreateConnection(endpoint);
            await using var consumer = await connection.CreateConsumerAsync("test-consumer", QueueRoutingType.Anycast, "q1");

            Assert.True(consumerAttached.WaitOne(Timeout));
            Assert.NotNull(attachFrame);
            Assert.IsType<Source>(attachFrame.Source);
            var sourceFrame = (Source) attachFrame.Source;
            Assert.Equal("test-consumer::q1", sourceFrame.Address);
            Assert.Contains(sourceFrame.Capabilities, RoutingCapabilities.Anycast.Equals);
        }

        [Fact]
        public async Task Should_throw_exception_when_selected_queue_doesnt_exist()
        {
            var endpoint = GetUniqueEndpoint();
            
            var testHandler = new TestHandler(@event =>
            {
                switch (@event.Id)
                {
                    case EventId.LinkLocalOpen when @event.Context is Attach attach:
                        attach.Source = null;
                        Task.Run(async () =>
                        {
                            await Task.Delay(TimeSpan.FromMilliseconds(5));
                            await @event.Link.CloseAsync(System.Threading.Timeout.InfiniteTimeSpan, new Error(ErrorCode.NotFound)
                            {
                                Description = "Queue: 'q1' does not exist"
                            });
                        });
                        break;
                }
            });

            using var host = CreateOpenedContainerHost(endpoint, testHandler);
            host.Open();

            await using var connection = await CreateConnection(endpoint);
            var exception = await Assert.ThrowsAsync<CreateConsumerException>(() => connection.CreateConsumerAsync("a1", QueueRoutingType.Anycast, "q1"));
            Assert.Contains("Queue: 'q1' does not exist", exception.Message);
            Assert.Equal(ErrorCode.NotFound, exception.Condition);
        }
        
        [Fact]
        public async Task Should_cancel_CreateConsumerAsync_when_address_routing_type_and_queue_specified_but_attach_frame_not_received_on_time()
        {
            var endpoint = GetUniqueEndpoint();

            using var host = CreateContainerHostThatWillNeverSendAttachFrameBack(endpoint);

            await using var connection = await CreateConnection(endpoint);
            var cancellationTokenSource = new CancellationTokenSource(ShortTimeout);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await connection.CreateConsumerAsync("a1", QueueRoutingType.Anycast, "q1", cancellationTokenSource.Token));
        }

        [Fact]
        public async Task Should_cancel_CreateConsumerAsync_when_address_and_routing_type_specified_but_attach_frame_not_received_on_time()
        {
            var endpoint = GetUniqueEndpoint();

            using var host = CreateContainerHostThatWillNeverSendAttachFrameBack(endpoint);

            await using var connection = await CreateConnection(endpoint);
            var cancellationTokenSource = new CancellationTokenSource(ShortTimeout);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async() => await connection.CreateConsumerAsync("test-consumer", QueueRoutingType.Anycast, cancellationTokenSource.Token));
        }
        
        [Fact]
        public async Task Should_cancel_CreateConsumerAsync_when_address_specified_but_attach_frame_not_received_on_time()
        {
            var endpoint = GetUniqueEndpoint();

            using var host = CreateContainerHostThatWillNeverSendAttachFrameBack(endpoint);

            await using var connection = await CreateConnection(endpoint);
            var cancellationTokenSource = new CancellationTokenSource(ShortTimeout);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async() => await connection.CreateConsumerAsync("test-consumer", QueueRoutingType.Anycast, cancellationTokenSource.Token));
        }
    }
}