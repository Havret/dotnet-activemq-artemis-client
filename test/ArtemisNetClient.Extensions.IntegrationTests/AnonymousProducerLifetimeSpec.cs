using System;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace ActiveMQ.Artemis.Client.Extensions.AspNetCore.IntegrationTests
{
    public class AnonymousProducerLifetimeSpec
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public AnonymousProducerLifetimeSpec(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }
        
        [Fact]
        public async Task Should_register_producer_with_transient_service_lifetime_by_default_1()
        {
            await ShouldRegisterProducerWithTransientServiceLifetimeByDefault(builder => builder.AddAnonymousProducer<TestProducer>());
        }

        [Fact]
        public async Task Should_register_producer_with_transient_service_lifetime_by_default_2()
        {
            await ShouldRegisterProducerWithTransientServiceLifetimeByDefault(builder => builder.AddAnonymousProducer<TestProducer>(new ProducerOptions
            {
                MessagePriority = 9
            }));
        }

        private async Task ShouldRegisterProducerWithTransientServiceLifetimeByDefault(Action<IActiveMqBuilder> registerProducerAction)
        {
            await using var testFixture = await TestFixture.CreateAsync(_testOutputHelper, registerProducerAction);

            var typedProducer1 = testFixture.Services.GetService<TestProducer>();
            var typedProducer2 = testFixture.Services.GetService<TestProducer>();

            Assert.NotEqual(typedProducer1, typedProducer2);
            Assert.Equal(typedProducer1.Producer, typedProducer2.Producer);
        }

        [Fact]
        public async Task Should_register_producer_with_singleton_service_lifetime_1()
        {
            await ShouldRegisterProducerWithSingletonServiceLifetime(builder => builder.AddAnonymousProducer<TestProducer>(ServiceLifetime.Singleton));
        }

        [Fact]
        public async Task Should_register_producer_with_singleton_service_lifetime_2()
        {
            await ShouldRegisterProducerWithSingletonServiceLifetime(builder => builder.AddAnonymousProducer<TestProducer>(new ProducerOptions
            {
                MessagePriority = 9
            }, ServiceLifetime.Singleton));
        }

        private async Task ShouldRegisterProducerWithSingletonServiceLifetime(Action<IActiveMqBuilder> registerProducerAction)
        {
            await using var testFixture = await TestFixture.CreateAsync(_testOutputHelper, registerProducerAction);

            var typedProducer1 = testFixture.Services.GetService<TestProducer>();
            var typedProducer2 = testFixture.Services.GetService<TestProducer>();

            Assert.Equal(typedProducer1, typedProducer2);
            Assert.Equal(typedProducer1.Producer, typedProducer2.Producer);
        }

        [Fact]
        public async Task Should_register_producer_with_scoped_service_lifetime_1()
        {
            await ShouldRegisterProducerWithScopedServiceLifetime(builder => builder.AddAnonymousProducer<TestProducer>(ServiceLifetime.Scoped));
        }

        [Fact]
        public async Task Should_register_producer_with_scoped_service_lifetime_2()
        {
            await ShouldRegisterProducerWithScopedServiceLifetime(builder => builder.AddAnonymousProducer<TestProducer>(new ProducerOptions
            {
                MessagePriority = 9
            }, ServiceLifetime.Scoped));
        }

        private async Task ShouldRegisterProducerWithScopedServiceLifetime(Action<IActiveMqBuilder> registerProducerAction)
        {
            await using var testFixture = await TestFixture.CreateAsync(_testOutputHelper, registerProducerAction);

            using var scope = testFixture.Services.CreateScope();
            var typedProducer1Scope1 = scope.ServiceProvider.GetService<TestProducer>();
            var typedProducer2Scope1 = scope.ServiceProvider.GetService<TestProducer>();

            using var scope2 = testFixture.Services.CreateScope();
            var typedProducerScope2 = scope2.ServiceProvider.GetService<TestProducer>();

            Assert.Equal(typedProducer1Scope1, typedProducer2Scope1);
            Assert.Equal(typedProducer1Scope1.Producer, typedProducer2Scope1.Producer);
            Assert.NotEqual(typedProducerScope2, typedProducer1Scope1);
            Assert.Equal(typedProducerScope2.Producer, typedProducer2Scope1.Producer);
        }

        private class TestProducer
        {
            public IAnonymousProducer Producer { get; }
            public TestProducer(IAnonymousProducer producer) => Producer = producer;
        }
    }
}