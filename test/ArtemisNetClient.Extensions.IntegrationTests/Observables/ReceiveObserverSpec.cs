using System;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace ActiveMQ.Artemis.Client.Extensions.AspNetCore.IntegrationTests.Observables
{
    public class ReceiveObserverSpec
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public ReceiveObserverSpec(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public async Task Should_call_PreReceive_and_PostReceive_when_message_received()
        {
            var receiveObserver = new TestReceiveObserver();
            var address = Guid.NewGuid().ToString();
            await using var testFixture = await TestFixture.CreateAsync(_testOutputHelper, builder =>
            {
                builder.Services.AddSingleton(receiveObserver);
                builder.AddConsumer(address, RoutingType.Multicast, async (message, consumer, _, _) =>
                {
                    await consumer.AcceptAsync(message);
                });
                builder.AddReceiveObserver<TestReceiveObserver>();
            });

            var producer = await testFixture.Connection.CreateProducerAsync(address, RoutingType.Multicast, testFixture.CancellationToken);
            await producer.SendAsync(new Message("foo"), testFixture.CancellationToken);

            Assert.Equal(receiveObserver.PreReceiveCalled.Task, await Task.WhenAny(receiveObserver.PreReceiveCalled.Task, Task.Delay(TimeSpan.FromSeconds(5))));
            Assert.Equal(receiveObserver.PostReceiveCalled.Task, await Task.WhenAny(receiveObserver.PostReceiveCalled.Task, Task.Delay(TimeSpan.FromSeconds(5))));
        }

        private class TestReceiveObserver : IReceiveObserver
        {
            public TaskCompletionSource<bool> PreReceiveCalled { get; } = new TaskCompletionSource<bool>();
            public void PreReceive(string address, RoutingType routingType, string queue, Message message) => PreReceiveCalled.TrySetResult(true);

            public TaskCompletionSource<bool> PostReceiveCalled { get; } = new TaskCompletionSource<bool>();
            public void PostReceive(string address, RoutingType routingType, string queue, Message message) => PostReceiveCalled.TrySetResult(true);
        }
    }
}