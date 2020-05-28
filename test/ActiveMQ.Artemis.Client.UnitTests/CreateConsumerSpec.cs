using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Exceptions;
using ActiveMQ.Artemis.Client.UnitTests.Utils;
using Amqp;
using Amqp.Framing;
using Amqp.Handler;
using Amqp.Types;
using Xunit;
using Xunit.Abstractions;

namespace ActiveMQ.Artemis.Client.UnitTests
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
            var consumer = await connection.CreateConsumerAsync("test-consumer", RoutingType.Anycast);
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
            await using var consumer = await connection.CreateConsumerAsync("test-consumer", RoutingType.Anycast);

            Assert.True(consumerAttached.WaitOne(Timeout));
            Assert.IsType<Source>(attachFrame.Source);
            Assert.Equal("test-consumer", ((Source) attachFrame.Source).Address);
        }

        [Theory, MemberData(nameof(RoutingTypesData))]
        public async Task Should_attach_to_address_with_specified_RoutingType(RoutingType routingType, Symbol routingCapability)
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
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => connection.CreateConsumerAsync("test-consumer", (RoutingType) 99));
        }

        [Fact]
        public async Task Should_connect_to_a_custom_queue_on_specified_address_without_routing_type()
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
            await using var consumer = await connection.CreateConsumerAsync("test-consumer", "q1");

            Assert.True(consumerAttached.WaitOne(Timeout));
            Assert.NotNull(attachFrame);
            Assert.IsType<Source>(attachFrame.Source);
            var sourceFrame = (Source) attachFrame.Source;
            Assert.Equal("test-consumer::q1", sourceFrame.Address);
            Assert.Equal("q1", attachFrame.LinkName);
            Assert.Null(sourceFrame.Capabilities);
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
            var exception = await Assert.ThrowsAsync<CreateConsumerException>(() => connection.CreateConsumerAsync("a1", "q1"));
            Assert.Contains("Queue: 'q1' does not exist", (string) exception.Message);
            Assert.Equal(ErrorCode.NotFound, exception.ErrorCode);
        }

        [Fact]
        public async Task Should_cancel_CreateConsumerAsync_when_attach_frame_not_received_on_time()
        {
            var endpoint = GetUniqueEndpoint();

            using var host = CreateContainerHostThatWillNeverSendAttachFrameBack(endpoint);

            await using var connection = await CreateConnection(endpoint);
            var cancellationTokenSource = new CancellationTokenSource(ShortTimeout);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await connection.CreateConsumerAsync("a1", RoutingType.Multicast, "q1", cancellationTokenSource.Token));
        }

        [Fact]
        public async Task Throws_when_created_with_null_configuration()
        {
            var endpoint = GetUniqueEndpoint();
            using var host = CreateOpenedContainerHost(endpoint);

            await using var connection = await CreateConnection(endpoint);
            await Assert.ThrowsAsync<ArgumentNullException>(() => connection.CreateConsumerAsync(null));
        }

        [Fact]
        public async Task Throws_when_created_with_null_address()
        {
            var endpoint = GetUniqueEndpoint();
            using var host = CreateOpenedContainerHost(endpoint);

            await using var connection = await CreateConnection(endpoint);
            await Assert.ThrowsAsync<ArgumentNullException>(() => connection.CreateConsumerAsync(null, RoutingType.Anycast));
        }

        [Fact]
        public async Task Throws_when_created_with_empty_address()
        {
            var endpoint = GetUniqueEndpoint();
            using var host = CreateOpenedContainerHost(endpoint);

            await using var connection = await CreateConnection(endpoint);
            await Assert.ThrowsAsync<ArgumentNullException>(() => connection.CreateConsumerAsync(string.Empty, RoutingType.Anycast));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        public async Task Throws_when_created_with_credit_less_than_1(int credit)
        {
            var endpoint = GetUniqueEndpoint();
            using var host = CreateOpenedContainerHost(endpoint);
            await using var connection = await CreateConnection(endpoint);

            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => connection.CreateConsumerAsync(new ConsumerConfiguration
            {
                Address = "a1",
                RoutingType = RoutingType.Multicast,
                Credit = credit
            }));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(100)]
        public async Task Should_create_Consumer_when_credit_is_greater_or_equal_to_1(int credit)
        {
            var endpoint = GetUniqueEndpoint();
            using var host = CreateOpenedContainerHost(endpoint);
            await using var connection = await CreateConnection(endpoint);

            await using var consumer = await connection.CreateConsumerAsync(new ConsumerConfiguration
            {
                Address = "a1",
                RoutingType = RoutingType.Multicast,
                Credit = credit
            });
        }

        [Fact]
        public async Task Throws_when_created_with_queue_name_and_Anycast_routing_type()
        {
            var endpoint = GetUniqueEndpoint();
            using var host = CreateOpenedContainerHost(endpoint);
            await using var connection = await CreateConnection(endpoint);

            await Assert.ThrowsAsync<ArgumentException>(() => connection.CreateConsumerAsync(new ConsumerConfiguration
            {
                Address = "a1",
                Queue = "q1",
                RoutingType = RoutingType.Anycast,
            }));
        }

        [Fact]
        public async Task Throws_on_attempt_to_attach_to_non_shared_non_durable_queue()
        {
            var endpoint = GetUniqueEndpoint();
            using var host = CreateOpenedContainerHost(endpoint);
            await using var connection = await CreateConnection(endpoint);

            await Assert.ThrowsAsync<ArgumentException>(() => connection.CreateConsumerAsync(new ConsumerConfiguration
            {
                Address = "a1",
                Queue = "q1",
                RoutingType = RoutingType.Multicast,
                Shared = false,
                Durable = false
            }));
        }

        [Fact]
        public async Task Throws_when_created_with_intent_to_attach_to_pre_configured_queue_when_Durable_option_enabled()
        {
            var endpoint = GetUniqueEndpoint();
            using var host = CreateOpenedContainerHost(endpoint);
            await using var connection = await CreateConnection(endpoint);

            await Assert.ThrowsAsync<ArgumentException>(() => connection.CreateConsumerAsync(new ConsumerConfiguration
            {
                Address = "a1",
                Queue = "q1",
                Durable = true
            }));
        }

        [Fact]
        public async Task Throws_when_created_with_intent_to_attach_to_pre_configured_queue_when_Shared_option_enabled()
        {
            var endpoint = GetUniqueEndpoint();
            using var host = CreateOpenedContainerHost(endpoint);
            await using var connection = await CreateConnection(endpoint);

            await Assert.ThrowsAsync<ArgumentException>(() => connection.CreateConsumerAsync(new ConsumerConfiguration
            {
                Address = "a1",
                Queue = "q1",
                Shared = true
            }));
        }

        [Fact]
        public async Task Throws_when_created_with_intent_to_attach_to_pre_configured_queue_when_queue_name_not_provided()
        {
            var endpoint = GetUniqueEndpoint();
            using var host = CreateOpenedContainerHost(endpoint);
            await using var connection = await CreateConnection(endpoint);

            await Assert.ThrowsAsync<ArgumentNullException>(() => connection.CreateConsumerAsync(new ConsumerConfiguration
            {
                Address = "a1",
            }));
        }
    }
}