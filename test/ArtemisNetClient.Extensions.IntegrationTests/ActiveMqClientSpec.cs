using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Nito.AsyncEx;
using Xunit;
using Xunit.Abstractions;

namespace ActiveMQ.Artemis.Client.Extensions.AspNetCore.IntegrationTests
{
    public class ActiveMqClientSpec
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public ActiveMqClientSpec(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }
        
        [Fact]
        public async Task Should_start_and_stop_client_multiple_times()
        {
            var address = Guid.NewGuid().ToString();
            var asyncCountdownEvent = new AsyncCountdownEvent(2);
            var msgBag = new ConcurrentBag<Message>();
            async Task MessageHandler(Message msg, IConsumer consumer, IServiceProvider provider, CancellationToken cancellationToken)
            {
                msgBag.Add(msg);
                asyncCountdownEvent.Signal();
                await consumer.AcceptAsync(msg);
            }
            
            // STEP 1: Configure producers and consumers
            await using var testFixture = await TestFixture.CreateAsync(_testOutputHelper, addActiveMqHostedService: false, configureActiveMq: builder =>
            {
                builder.AddProducer<TestProducer>(address, RoutingType.Multicast);
                builder.AddAnonymousProducer<TestAnonymousProducer>();
                builder.AddConsumer(address, RoutingType.Multicast, MessageHandler);
            });

            // STEP 2: Start the client
            var activeMqClient = testFixture.Services.GetRequiredService<IActiveMqClient>();
            await activeMqClient.StartAsync(CancellationToken.None);
            
            // STEP 3: Send the first round of messages
            var testProducer = testFixture.Services.GetRequiredService<TestProducer>();
            var testAnonymousProducer = testFixture.Services.GetRequiredService<TestAnonymousProducer>();
            
            await testProducer.SendMessage("msg-1", CancellationToken.None);
            await testAnonymousProducer.SendMessage(address, RoutingType.Multicast, "msg-2", CancellationToken.None);

            await asyncCountdownEvent.WaitAsync();
            Assert.Equal(2, msgBag.Count);
            
            // STEP 4: Stop the client
            await activeMqClient.StopAsync(CancellationToken.None);
            
            // STEP 5: Make sure that the client was stopped
            await Assert.ThrowsAnyAsync<InvalidOperationException>(() => testProducer.SendMessage("msg-3", CancellationToken.None));
            await Assert.ThrowsAnyAsync<InvalidOperationException>(() => testAnonymousProducer.SendMessage(address, RoutingType.Multicast, "msg-4", CancellationToken.None));
            
            // STEP 6: Restart the client
            await activeMqClient.StartAsync(CancellationToken.None);
            asyncCountdownEvent.AddCount(2);

            // STEP 7: Send the second round of messages
            await testProducer.SendMessage("msg-5", CancellationToken.None);
            await testAnonymousProducer.SendMessage(address, RoutingType.Multicast, "msg-6", CancellationToken.None);
            
            await asyncCountdownEvent.WaitAsync();
            Assert.Equal(4, msgBag.Count);
        }

        private class TestProducer
        {
            private readonly IProducer _producer;

            public TestProducer(IProducer producer) => _producer = producer;
            public Task SendMessage(string text, CancellationToken cancellationToken) => _producer.SendAsync(new Message(text), cancellationToken);
        }
        
        private class TestAnonymousProducer
        {
            private readonly IAnonymousProducer _producer;

            public TestAnonymousProducer(IAnonymousProducer producer) => _producer = producer;
            public Task SendMessage(string address, RoutingType routingType, string text, CancellationToken cancellationToken)
            {
                return _producer.SendAsync(address, routingType, new Message(text), cancellationToken);
            }
        }
    }
}