using System;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace ActiveMQ.Artemis.Client.Extensions.AspNetCore.IntegrationTests
{
    public class RegisterConsumerSpec
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public RegisterConsumerSpec(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public async Task Should_register_consumer_with_given_configuration_1()
        {
            await ShouldRegisterConsumerWithGivenConfiguration((address, tcs, builder) =>
            {
                builder.AddConsumer(address, RoutingType.Anycast, async (message, consumer, token, serviceProvider) =>
                {
                    tcs.TrySetResult(message);
                    await consumer.AcceptAsync(message);
                });
            });
        }
        
        [Fact]
        public async Task Should_register_consumer_with_given_configuration_2()
        {
            await ShouldRegisterConsumerWithGivenConfiguration((address, tcs, builder) =>
            {
                builder.AddConsumer(address, RoutingType.Anycast, new ConsumerOptions { Credit = 100 }, async (message, consumer, token, serviceProvider) =>
                {
                    tcs.TrySetResult(message);
                    await consumer.AcceptAsync(message);
                });
            });
        }

        [Fact]
        public async Task Should_register_consumer_with_given_configuration_3()
        {
            await ShouldRegisterConsumerWithGivenConfiguration((address, tcs, builder) =>
            {
                builder.AddConsumer(address, RoutingType.Anycast, Guid.NewGuid().ToString(), async (message, consumer, token, serviceProvider) =>
                {
                    tcs.TrySetResult(message);
                    await consumer.AcceptAsync(message);
                });
            });
        }

        [Fact]
        public async Task Should_register_consumer_with_given_configuration_4()
        {
            await ShouldRegisterConsumerWithGivenConfiguration((address, tcs, builder) =>
            {
                builder.AddConsumer(address, RoutingType.Anycast, Guid.NewGuid().ToString(), new ConsumerOptions { Credit = 100 }, async (message, consumer, token, serviceProvider) =>
                {
                    tcs.TrySetResult(message);
                    await consumer.AcceptAsync(message);
                });
            });
        }

        [Fact]
        public async Task Should_register_consumer_with_given_configuration_5()
        {
            await ShouldRegisterConsumerWithGivenConfiguration((address, tcs, builder) =>
            {
                builder.AddConsumer(address, RoutingType.Anycast, Guid.NewGuid().ToString(), new QueueOptions { Exclusive = true }, async (message, consumer, token, serviceProvider) =>
                {
                    tcs.TrySetResult(message);
                    await consumer.AcceptAsync(message);
                });
            });
        }
        
        [Fact]
        public async Task Should_register_consumer_with_given_configuration_6()
        {
            await ShouldRegisterConsumerWithGivenConfiguration((address, tcs, builder) =>
            {
                builder.AddConsumer(address, RoutingType.Anycast, Guid.NewGuid().ToString(), new ConsumerOptions { Credit = 100 }, new QueueOptions { Exclusive = true }, async (message, consumer, token, serviceProvider) =>
                {
                    tcs.TrySetResult(message);
                    await consumer.AcceptAsync(message);
                });
            });
        }

        private async Task ShouldRegisterConsumerWithGivenConfiguration(Action<string, TaskCompletionSource<Message>, IActiveMqBuilder> configureConsumer)
        {
            var address = Guid.NewGuid().ToString();

            var tcs = new TaskCompletionSource<Message>();
            await using var testFixture = await TestFixture.CreateAsync(_testOutputHelper, activeMqBuilder =>
            {
                configureConsumer(address, tcs, activeMqBuilder);
                activeMqBuilder.EnableAddressDeclaration().EnableQueueDeclaration();
            });

            var producer = await testFixture.Connection.CreateProducerAsync(address, testFixture.CancellationToken);
            await producer.SendAsync(new Message("foo"), testFixture.CancellationToken);

            Assert.Equal("foo", (await tcs.Task).GetBody<string>());
        }
    }
}