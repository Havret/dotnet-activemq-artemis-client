using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Extensions.DependencyInjection;
using ActiveMQ.Artemis.Client.TestUtils;
using NScenario;
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

        [Fact]
        public async Task Should_create_shared_consumer()
        {
            var address = Guid.NewGuid().ToString();
            var queue = Guid.NewGuid().ToString();

            var dictionary = new ConcurrentDictionary<int, bool>();

            await using var testFixture = await TestFixture.CreateAsync(_testOutputHelper, builder =>
            {
                builder.AddSharedConsumer(address, queue, async (message, consumer, _, _) =>
                {
                    dictionary.TryAdd(1, true);
                    await consumer.AcceptAsync(message);
                });
                builder.AddSharedConsumer(address, queue, async (message, consumer, _, _) =>
                {
                    dictionary.TryAdd(2, true);
                    await consumer.AcceptAsync(message);
                });
            });

            await using var producer = await testFixture.Connection.CreateProducerAsync(address, RoutingType.Multicast, testFixture.CancellationToken);
            await producer.SendAsync(new Message("foo"), testFixture.CancellationToken);
            await producer.SendAsync(new Message("foo"), testFixture.CancellationToken);

            Assert.Equal(2, await Retry.RetryUntil(
                () => Task.FromResult(dictionary.Keys.Count),
                x => x == 2,
                TimeSpan.FromMilliseconds(100)));
        }
        
        [Fact]
        public async Task Should_create_shared_durable_consumer()
        {
            var scenario = TestScenarioFactory.Default(new XUnitOutputAdapter(_testOutputHelper));
            
            var address = Guid.NewGuid().ToString();
            var queue = Guid.NewGuid().ToString();
            var dictionary = new ConcurrentDictionary<int, bool>();
            
            var testFixture1 = await scenario.Step("Register two shared durable consumers", async () =>
            {
                return await TestFixture.CreateAsync(_testOutputHelper, builder =>
                {
                    builder.AddSharedDurableConsumer(address, queue, (_, _, _, _) =>
                    {
                        dictionary.TryAdd(1, true);
                        return Task.CompletedTask;
                    });
                    builder.AddSharedDurableConsumer(address, queue, (_, _, _, _) =>
                    {
                        dictionary.TryAdd(2, true);
                        return Task.CompletedTask;
                    });
                });
            });

            await scenario.Step("Send two messages", async () =>
            {
                await using var producer = await testFixture1.Connection.CreateProducerAsync(address, RoutingType.Multicast, testFixture1.CancellationToken);
                await producer.SendAsync(new Message("foo"), testFixture1.CancellationToken);
                await producer.SendAsync(new Message("foo"), testFixture1.CancellationToken);
            });

            await scenario.Step("Verify that messages were distributed among two consumers", async () =>
            {
                Assert.Equal(2, await Retry.RetryUntil(
                    func: () => Task.FromResult(dictionary.Keys.Count),
                    until: x => x == 2,
                    timeout: TimeSpan.FromMilliseconds(100)));
            });

            await scenario.Step("Close initial consumers without acknowledging messages", async () =>
            {
                await testFixture1.DisposeAsync();
                dictionary.Clear();
            });
            
            await using var testFixture2 = await scenario.Step("Register a new consumer", async () =>
            {
                return await TestFixture.CreateAsync(_testOutputHelper, builder =>
                {
                    builder.AddSharedDurableConsumer(address, queue, async (message, consumer, _, _) =>
                    {
                        dictionary.TryAdd(1, true);
                        await consumer.AcceptAsync(message);
                    });
                });
            });
            
            await scenario.Step("Verify that messages were redelivered to the new consumer - queue was durable", async () =>
            {
                Assert.Equal(1, await Retry.RetryUntil(
                    func: () => Task.FromResult(dictionary.Keys.Count),
                    until: x => x == 1,
                    timeout: TimeSpan.FromMilliseconds(100)));
            });
        }
    }
}