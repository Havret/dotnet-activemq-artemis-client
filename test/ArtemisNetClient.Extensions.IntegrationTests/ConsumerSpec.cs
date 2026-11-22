using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Exceptions;
using ActiveMQ.Artemis.Client.Extensions.DependencyInjection;
using ActiveMQ.Artemis.Client.TestUtils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
        public async Task Should_continue_receiving_messages_after_message_handler_throws_consumer_closed_exception()
        {
            var address = Guid.NewGuid().ToString();

            var tcs = new TaskCompletionSource<Message>();
            await using var testFixture = await TestFixture.CreateAsync(_testOutputHelper, builder =>
            {
                builder.EnableAddressDeclaration()
                       .EnableQueueDeclaration()
                       .AddConsumer(address, RoutingType.Anycast, async (message, consumer, _, _) =>
                       {
                           if (message.GetBody<string>() == "foo")
                           {
                               // Only the consumer's own receive tells whether it's closed for good, e.g. this one may come from another consumer
                               throw new ConsumerClosedException();
                           }

                           await consumer.AcceptAsync(message);
                           tcs.TrySetResult(message);
                       });
            });

            await using var producer = await testFixture.Connection.CreateProducerAsync(address, RoutingType.Anycast, testFixture.CancellationToken);
            await producer.SendAsync(new Message("foo"), testFixture.CancellationToken);
            await producer.SendAsync(new Message("bar"), testFixture.CancellationToken);

            var result = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(5)));

            Assert.Equal(tcs.Task, result);
            Assert.Equal("bar", (await tcs.Task).GetBody<string>());
        }

        [Fact]
        public async Task Should_stop_receiving_messages_once_connection_disposed()
        {
            var address = Guid.NewGuid().ToString();
            var loggerProvider = new ErrorCollectingLoggerProvider("ActiveMQ.Artemis.Client.Extensions.DependencyInjection.ActiveMqConsumer");

            await using var testFixture = await TestFixture.CreateAsync(_testOutputHelper, builder =>
            {
                builder.EnableAddressDeclaration()
                       .EnableQueueDeclaration()
                       .AddConsumer(address, RoutingType.Anycast, async (message, consumer, _, _) => await consumer.AcceptAsync(message));
            }, services => services.AddSingleton<ILoggerProvider>(loggerProvider));

            var connection = await testFixture.Services.GetRequiredService<ConnectionProvider>().GetConnectionAsync("my-test-artemis", testFixture.CancellationToken);
            await connection.DisposeAsync();

            // Give a spinning receive loop the time to show up
            await Task.Delay(TimeSpan.FromMilliseconds(500));

            Assert.Equal(new[] { "Consumer was closed. No more messages will be received." }, loggerProvider.Errors);
        }

        private class ErrorCollectingLoggerProvider : ILoggerProvider, ILogger
        {
            private readonly string _categoryName;
            private readonly ConcurrentQueue<string> _errors = new();

            public ErrorCollectingLoggerProvider(string categoryName) => _categoryName = categoryName;

            public string[] Errors => _errors.ToArray();

            public ILogger CreateLogger(string categoryName) => categoryName == _categoryName ? this : Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                if (logLevel >= LogLevel.Error)
                {
                    _errors.Enqueue(formatter(state, exception));
                }
            }

            public bool IsEnabled(LogLevel logLevel) => true;

            public IDisposable BeginScope<TState>(TState state) => null;

            public void Dispose()
            {
            }
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