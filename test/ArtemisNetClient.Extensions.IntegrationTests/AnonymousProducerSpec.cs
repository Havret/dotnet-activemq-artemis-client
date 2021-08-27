using System;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace ActiveMQ.Artemis.Client.Extensions.AspNetCore.IntegrationTests
{
    public class AnonymousProducerSpec
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public AnonymousProducerSpec(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public async Task Should_register_anonymous_producer()
        {
            await using var testFixture = await TestFixture.CreateAsync(_testOutputHelper, activeMqBuilder => { activeMqBuilder.AddAnonymousProducer<TestProducer>(); });

            var testProducer = testFixture.Services.GetRequiredService<TestProducer>();

            Assert.NotNull(testProducer);
        }

        [Fact]
        public async Task Should_send_messages_using_registered_producer()
        {
            var address1 = Guid.NewGuid().ToString();
            var address2 = Guid.NewGuid().ToString();

            await using var testFixture = await TestFixture.CreateAsync(_testOutputHelper, activeMqBuilder => { activeMqBuilder.AddAnonymousProducer<TestProducer>(); });

            var consumer1 = await testFixture.Connection.CreateConsumerAsync(address1, RoutingType.Anycast, cancellationToken: testFixture.CancellationToken);
            var consumer2 = await testFixture.Connection.CreateConsumerAsync(address2, RoutingType.Multicast, cancellationToken: testFixture.CancellationToken);

            var testProducer = testFixture.Services.GetRequiredService<TestProducer>();
            await testProducer.SendMessage(address1, RoutingType.Anycast, "foo1", testFixture.CancellationToken);
            await testProducer.SendMessage(address2, RoutingType.Multicast, "foo2", testFixture.CancellationToken);

            Assert.Equal("foo1", (await consumer1.ReceiveAsync(testFixture.CancellationToken)).GetBody<string>());
            Assert.Equal("foo2", (await consumer2.ReceiveAsync(testFixture.CancellationToken)).GetBody<string>());
        }

        [Fact]
        public async Task Throws_when_producer_with_the_same_type_registered_twice()
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => TestFixture.CreateAsync(_testOutputHelper, activeMqBuilder =>
            {
                activeMqBuilder.AddAnonymousProducer<TestProducer>();
                activeMqBuilder.AddAnonymousProducer<TestProducer>();
            }));

            Assert.Contains($"There has already been registered Anonymous Producer with the type '{typeof(TestProducer).FullName}'", exception.Message);
        }

        [Fact]
        public async Task Should_register_configured_producer()
        {
            var address1 = Guid.NewGuid().ToString();

            await using var testFixture = await TestFixture.CreateAsync(_testOutputHelper, activeMqBuilder =>
            {
                activeMqBuilder.AddAnonymousProducer<TestProducer>(new ProducerOptions
                {
                    MessagePriority = 9
                });
            });

            var consumer = await testFixture.Connection.CreateConsumerAsync(address1, RoutingType.Anycast, testFixture.CancellationToken);

            var testProducer = testFixture.Services.GetRequiredService<TestProducer>();
            await testProducer.SendMessage(address1, RoutingType.Anycast, "foo", testFixture.CancellationToken);

            var msg = await consumer.ReceiveAsync(testFixture.CancellationToken);
            Assert.Equal((byte?) 9, msg.Priority);
        }

        private class TestProducer
        {
            private readonly IAnonymousProducer _producer;

            public TestProducer(IAnonymousProducer producer) => _producer = producer;

            public Task SendMessage(string address, RoutingType routingType, string text, CancellationToken cancellationToken)
            {
                return _producer.SendAsync(address, routingType, new Message(text), cancellationToken);
            }
        }

        [Fact]
        public async Task Should_register_multiple_producers()
        {
            await using var testFixture = await TestFixture.CreateAsync(_testOutputHelper, activeMqBuilder =>
            {
                activeMqBuilder.AddAnonymousProducer<TestProducer1>();
                activeMqBuilder.AddAnonymousProducer<TestProducer2>();
            });

            var testProducer1 = testFixture.Services.GetRequiredService<TestProducer1>();
            var testProducer2 = testFixture.Services.GetRequiredService<TestProducer2>();

            Assert.NotEqual(testProducer1.Producer, testProducer2.Producer);
        }

        private class TestProducer1
        {
            public IAnonymousProducer Producer { get; }
            public TestProducer1(IAnonymousProducer producer) => Producer = producer;
        }

        private class TestProducer2
        {
            public IAnonymousProducer Producer { get; }
            public TestProducer2(IAnonymousProducer producer) => Producer = producer;
        }
    }
}