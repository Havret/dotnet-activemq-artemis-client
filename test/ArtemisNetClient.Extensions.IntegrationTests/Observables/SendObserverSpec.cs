using System;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace ActiveMQ.Artemis.Client.Extensions.AspNetCore.IntegrationTests.Observables
{
    public class SendObserverSpec
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public SendObserverSpec(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public async Task Should_call_PreSend_and_PostSend_on_message_SendAsync()
        {
            var sendObserver = new TestSendObserver();
            var address = Guid.NewGuid().ToString();
            await using var testFixture = await TestFixture.CreateAsync(_testOutputHelper, builder =>
            {
                builder.Services.AddSingleton(sendObserver);
                builder.AddProducer<TestProducer>(address)
                       .AddSendObserver<TestSendObserver>();
            });

            var testProducer = testFixture.Services.GetService<TestProducer>();
            await testProducer.Producer.SendAsync(new Message("foo"), testFixture.CancellationToken);

            Assert.True(sendObserver.PreSendCalled);
            Assert.True(sendObserver.PostSendCalled);
        }
        
        [Fact]
        public async Task Should_call_PreSend_and_PostSend_on_message_Send()
        {
            var sendObserver = new TestSendObserver();
            var address = Guid.NewGuid().ToString();
            await using var testFixture = await TestFixture.CreateAsync(_testOutputHelper, builder =>
            {
                builder.Services.AddSingleton(sendObserver);
                builder.AddProducer<TestProducer>(address)
                       .AddSendObserver<TestSendObserver>();
            });

            var testProducer = testFixture.Services.GetService<TestProducer>();
            testProducer.Producer.Send(new Message("foo"), testFixture.CancellationToken);

            Assert.True(sendObserver.PreSendCalled);
            Assert.True(sendObserver.PostSendCalled);
        }

        private class TestProducer
        {
            public IProducer Producer { get; }
            public TestProducer(IProducer producer) => Producer = producer;
        }

        private class TestSendObserver : ISendObserver
        {
            public bool PreSendCalled { get; private set; }
            public void PreSend(string address, RoutingType? routingType, Message message) => PreSendCalled = true;

            public bool PostSendCalled { get; private set; }
            public void PostSend(string address, RoutingType? routingType, Message message) => PostSendCalled = true;
        }
    }
}