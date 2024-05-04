﻿using ActiveMQ.Artemis.Client.Exceptions;
using ActiveMQ.Artemis.Client.UnitTests.Utils;
using Amqp;
using Amqp.Framing;
using Amqp.Handler;
using Amqp.Types;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace ActiveMQ.Artemis.Client.UnitTests
{
    public class CreateBrowserSpec : ActiveMQNetSpec
    {
        public CreateBrowserSpec(ITestOutputHelper output) : base(output)
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

            var configuration = new ConsumerConfiguration
            {
                Address = "test-consumer",
                RoutingType = RoutingType.Anycast
            };

            var browser = await connection.CreateBrowserAsync(configuration);

            await browser.DisposeAsync();

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

            var configuration = new ConsumerConfiguration
            {
                Address = "test-consumer",
                RoutingType = RoutingType.Anycast
            };

            await connection.CreateBrowserAsync(configuration);

            Assert.True(consumerAttached.WaitOne(Timeout));
            Assert.IsType<Source>(attachFrame.Source);
            Assert.Equal("test-consumer", ((Source)attachFrame.Source).Address);
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

            var configuration = new ConsumerConfiguration
            {
                Address = "test-consumer",
                RoutingType = routingType
            };

            await connection.CreateBrowserAsync(configuration);

            Assert.True(consumerAttached.WaitOne(Timeout));
            Assert.NotNull(attachFrame);
            Assert.IsType<Source>(attachFrame.Source);
            Assert.Contains(((Source)attachFrame.Source).Capabilities, routingCapability.Equals);
        }

        public static IEnumerable<object[]> RoutingTypesData()
        {
            return new[]
            {
                new object[] { RoutingType.Anycast, Capabilities.Anycast },
                new object[] { RoutingType.Multicast, Capabilities.Multicast }
            };
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

            var configuration = new ConsumerConfiguration
            {
                Address = "test-consumer",
                Queue = "q1"
            };

            await using var browser = await connection.CreateBrowserAsync(configuration);

            Assert.True(consumerAttached.WaitOne(Timeout));
            Assert.NotNull(attachFrame);
            Assert.IsType<Source>(attachFrame.Source);
            var sourceFrame = (Source)attachFrame.Source;
            Assert.Equal("test-consumer::q1", sourceFrame.Address);
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

            var configuration = new ConsumerConfiguration
            {
                Address = "a1",
                Queue = "q1"
            };

            var exception = await Assert.ThrowsAsync<CreateConsumerException>(() => connection.CreateBrowserAsync(configuration));
            Assert.Contains("Queue: 'q1' does not exist", exception.Message);
            Assert.Equal(ErrorCode.NotFound, exception.ErrorCode);
        }

        [Fact]
        public async Task Should_cancel_CreateBrowserAsync_when_attach_frame_not_received_on_time()
        {
            var endpoint = GetUniqueEndpoint();

            using var host = CreateContainerHostThatWillNeverSendAttachFrameBack(endpoint);

            await using var connection = await CreateConnection(endpoint);
            var cancellationTokenSource = new CancellationTokenSource(ShortTimeout);

            var configuration = new ConsumerConfiguration
            {
                Address = "a1",
                Queue = "q1",
            };

            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await connection.CreateBrowserAsync(configuration, cancellationTokenSource.Token));
        }

        [Fact]
        public async Task Throws_when_created_with_null_configuration()
        {
            var endpoint = GetUniqueEndpoint();
            using var host = CreateOpenedContainerHost(endpoint);

            await using var connection = await CreateConnection(endpoint);
            await Assert.ThrowsAsync<ArgumentNullException>(() => connection.CreateBrowserAsync(null));
        }

        [Fact]
        public async Task Throws_when_created_with_null_address()
        {
            var endpoint = GetUniqueEndpoint();
            using var host = CreateOpenedContainerHost(endpoint);

            var configuration = new ConsumerConfiguration
            {
                Address = null
            };

            await using var connection = await CreateConnection(endpoint);
            await Assert.ThrowsAsync<ArgumentNullException>(() => connection.CreateBrowserAsync(configuration));
        }

        [Fact]
        public async Task Throws_when_created_with_empty_address()
        {
            var endpoint = GetUniqueEndpoint();
            using var host = CreateOpenedContainerHost(endpoint);

            var configuration = new ConsumerConfiguration
            {
                Address = string.Empty
            };

            await using var connection = await CreateConnection(endpoint);
            await Assert.ThrowsAsync<ArgumentNullException>(() => connection.CreateBrowserAsync(configuration));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        public async Task Throws_when_created_with_credit_less_than_1(int credit)
        {
            var endpoint = GetUniqueEndpoint();
            using var host = CreateOpenedContainerHost(endpoint);
            await using var connection = await CreateConnection(endpoint);

            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => connection.CreateBrowserAsync(new ConsumerConfiguration
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

            await using var consumer = await connection.CreateBrowserAsync(new ConsumerConfiguration
            {
                Address = "a1",
                RoutingType = RoutingType.Multicast,
                Credit = credit
            });
        }

        [Theory]
        [InlineData(RoutingType.Anycast)]
        [InlineData(RoutingType.Multicast)]
        public async Task Throws_when_created_with_queue_name_and_routing_type(RoutingType routingType)
        {
            var endpoint = GetUniqueEndpoint();
            using var host = CreateOpenedContainerHost(endpoint);
            await using var connection = await CreateConnection(endpoint);

            await Assert.ThrowsAsync<ArgumentException>(() => connection.CreateBrowserAsync(new ConsumerConfiguration
            {
                Address = "a1",
                Queue = "q1",
                RoutingType = routingType,
            }));
        }

        [Fact]
        public async Task Throws_when_queue_name_not_provided()
        {
            var endpoint = GetUniqueEndpoint();
            using var host = CreateOpenedContainerHost(endpoint);
            await using var connection = await CreateConnection(endpoint);

            await Assert.ThrowsAsync<ArgumentNullException>(() => connection.CreateBrowserAsync(new ConsumerConfiguration
            {
                Address = "a1",
            }));
        }
    }
}