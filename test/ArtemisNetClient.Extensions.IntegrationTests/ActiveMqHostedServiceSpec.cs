using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Extensions.DependencyInjection;
using ActiveMQ.Artemis.Client.Extensions.Hosting;
using ActiveMQ.Artemis.Client.TestUtils;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace ActiveMQ.Artemis.Client.Extensions.AspNetCore.IntegrationTests
{
    public class ActiveMqHostedServiceSpec
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public ActiveMqHostedServiceSpec(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public async Task Should_block_host_startup_by_default_when_broker_is_unavailable()
        {
            var address = Guid.NewGuid().ToString();
            var clientEndpoints = new[] { EndpointUtil.GetUniqueEndpoint() };

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => TestFixture.CreateAsync(_testOutputHelper, clientEndpoints: clientEndpoints, configureActiveMq: builder =>
            {
                builder.AddProducer<TestProducer>(address, RoutingType.Anycast);
            }));
        }

        [Fact]
        public async Task Should_start_consumers_and_producers_in_the_background()
        {
            var requestAddress = Guid.NewGuid().ToString();
            var replyAddress = Guid.NewGuid().ToString();
            var hostedServiceOptions = new ActiveMqHostedServiceOptions { WaitUntilStarted = false };
            await using var testFixture = await TestFixture.CreateAsync(_testOutputHelper, hostedServiceOptions: hostedServiceOptions, configureActiveMq: builder =>
            {
                builder.AddConsumer(requestAddress, RoutingType.Anycast, async (message, consumer, serviceProvider, cancellationToken) =>
                {
                    var producer = serviceProvider.GetRequiredService<TestProducer>();
                    await producer.SendMessage(message.GetBody<string>() + "-reply", cancellationToken);
                    await consumer.AcceptAsync(message);
                });
                builder.AddProducer<TestProducer>(replyAddress, RoutingType.Anycast);
            });
            await using var producer = await testFixture.Connection.CreateProducerAsync(requestAddress, RoutingType.Anycast, testFixture.CancellationToken);
            await using var consumer = await testFixture.Connection.CreateConsumerAsync(replyAddress, RoutingType.Anycast, testFixture.CancellationToken);

            await producer.SendAsync(new Message("foo"), testFixture.CancellationToken);

            var reply = await consumer.ReceiveAsync(testFixture.CancellationToken);
            Assert.Equal("foo-reply", reply.GetBody<string>());
        }

        [Fact]
        public async Task Should_wait_for_producer_to_start_when_auto_recovery_is_enabled()
        {
            var address = Guid.NewGuid().ToString();
            var clientEndpoints = new[] { EndpointUtil.GetUniqueEndpoint() };
            var hostedServiceOptions = new ActiveMqHostedServiceOptions { WaitUntilStarted = false };
            await using var testFixture = await TestFixture.CreateAsync(_testOutputHelper, clientEndpoints: clientEndpoints, hostedServiceOptions: hostedServiceOptions, configureActiveMq: builder =>
            {
                builder.AddProducer<TestProducer>(address, RoutingType.Anycast);
            });
            var producer = testFixture.Services.GetRequiredService<TestProducer>();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => producer.SendMessage("foo", new CancellationTokenSource(TimeSpan.FromMilliseconds(500)).Token));
        }

        [Fact]
        public async Task Should_throw_connection_exception_when_auto_recovery_is_disabled()
        {
            var address = Guid.NewGuid().ToString();
            var clientEndpoints = new[] { EndpointUtil.GetUniqueEndpoint() };
            var hostedServiceOptions = new ActiveMqHostedServiceOptions { WaitUntilStarted = false };
            await using var testFixture = await TestFixture.CreateAsync(_testOutputHelper, clientEndpoints: clientEndpoints, hostedServiceOptions: hostedServiceOptions, configureActiveMq: builder =>
            {
                builder.ConfigureConnectionFactory((_, factory) => factory.AutomaticRecoveryEnabled = false);
                builder.AddProducer<TestProducer>(address, RoutingType.Anycast);
            });
            var producer = testFixture.Services.GetRequiredService<TestProducer>();

            await Assert.ThrowsAsync<SocketException>(() => producer.SendMessage("foo", CancellationToken.None));
        }

        private class TestProducer
        {
            private readonly IProducer _producer;

            public TestProducer(IProducer producer) => _producer = producer;
            public Task SendMessage(string text, CancellationToken cancellationToken) => _producer.SendAsync(new Message(text), cancellationToken);
        }
    }
}