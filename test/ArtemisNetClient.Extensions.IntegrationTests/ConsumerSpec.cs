using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace ActiveMQ.Artemis.Client.Extensions.AspNetCore.IntegrationTests
{
    public class ConsumerSpec
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public ConsumerSpec(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public async Task Should_create_multiple_concurrent_consumers()
        {
            var address = Guid.NewGuid().ToString();
            var queue = Guid.NewGuid().ToString();

            var consumers = new ConcurrentBag<IConsumer>();

            async Task MessageHandler(Message message, IConsumer consumer, IServiceProvider provider, CancellationToken token)
            {
                consumers.Add(consumer);
                await consumer.AcceptAsync(message);
            }

            await using var testFixture = await TestFixture.CreateAsync(_testOutputHelper, builder =>
            {
                builder.AddConsumer(address, RoutingType.Multicast, queue, new ConsumerOptions { ConcurrentConsumers = 3 }, MessageHandler)
                       .EnableAddressDeclaration()
                       .EnableQueueDeclaration();
            });

            await using var producer = await testFixture.Connection.CreateProducerAsync(address, RoutingType.Multicast, testFixture.CancellationToken);
            for (int i = 0; i < 100; i++)
            {
                await producer.SendAsync(new Message("foo" + i), testFixture.CancellationToken);
            }

            Assert.Equal(3, consumers.Distinct().Count());
        }

        [Fact]
        public async Task Should_be_able_to_stop_application_during_message_processing()
        {
            var address = Guid.NewGuid().ToString();
            var queue = Guid.NewGuid().ToString();

            var testFixture = await TestFixture.CreateAsync(_testOutputHelper, builder =>
            {
                builder.EnableAddressDeclaration()
                       .EnableQueueDeclaration()
                       .AddConsumer(address, RoutingType.Multicast, queue, async (message, consumer, _, token) =>
                       {
                           await Task.Delay(TimeSpan.FromMinutes(10), token);
                           await consumer.AcceptAsync(message);
                       });
            });

            await using var producer = await testFixture.Connection.CreateProducerAsync(address, RoutingType.Multicast, testFixture.CancellationToken);
            await producer.SendAsync(new Message("foo"), testFixture.CancellationToken);

            var stopHostTask = Task.Run(async () => await testFixture.DisposeAsync());
            var result = await Task.WhenAny(stopHostTask, Task.Delay(TimeSpan.FromSeconds(5)));

            Assert.Equal(stopHostTask, result);
        }
    }
}