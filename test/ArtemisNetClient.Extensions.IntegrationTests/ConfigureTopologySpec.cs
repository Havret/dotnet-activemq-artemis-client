using System;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace ActiveMQ.Artemis.Client.Extensions.AspNetCore.IntegrationTests;

public class ConfigureTopologySpec
{
    private readonly ITestOutputHelper _testOutputHelper;

    public ConfigureTopologySpec(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public async Task Should_configure_broker_topology_using_registered_delegate()
    {
        var addressName = Guid.NewGuid().ToString();
        var queueName = Guid.NewGuid().ToString();

        await using var testFixture = await TestFixture.CreateAsync(_testOutputHelper, activeMqBuilder =>
        {
            activeMqBuilder.ConfigureTopology(async (_, topologyManager) =>
            {
                await topologyManager.CreateQueueAsync(new QueueConfiguration
                {
                    Address = addressName,
                    Name = queueName,
                    AutoCreateAddress = true
                });
            });
        });

        var topologyManager = await testFixture.Connection.CreateTopologyManagerAsync(testFixture.CancellationToken);
        var queueNames = await topologyManager.GetQueueNamesAsync(testFixture.CancellationToken);

        Assert.Contains(queueName, queueNames);
    }

    [Fact]
    public async Task Should_declare_queue_with_last_value_key_when_anycast_producer_is_registered_for_that_queue()
    {
        var addressName = Guid.NewGuid().ToString();

        await using var testFixture = await TestFixture.CreateAsync(_testOutputHelper, activeMqBuilder =>
        {
            activeMqBuilder.ConfigureTopology(async (_, topologyManager) =>
                {
                    await topologyManager.DeclareQueueAsync(new QueueConfiguration
                    {
                        Address = addressName,
                        Name = addressName,
                        RoutingType = RoutingType.Anycast,
                        AutoCreateAddress = true,
                        LastValueKey = "my-key"
                    });
                })
                .AddProducer<TestProducer>(address: addressName, RoutingType.Anycast);
        });

        // Send two messages with the same LastValueKey
        var producer = testFixture.Services.GetRequiredService<TestProducer>();
        await producer.Producer.SendAsync(new Message("foo") { ApplicationProperties = { ["my-key"] = "1" } });
        await producer.Producer.SendAsync(new Message("bar") { ApplicationProperties = { ["my-key"] = "1" } });
        
        // Verify that LastValueKey feature was applied for the queue
        // Only the last message should be available for the consumer
        await using var consumer = await testFixture.Connection.CreateConsumerAsync(address: addressName, RoutingType.Anycast);
        var msg = await consumer.ReceiveAsync(testFixture.CancellationToken);
        Assert.Equal("bar", msg.GetBody<string>());
    }
    
    private class TestProducer
    {
        public IProducer Producer { get; }
        public TestProducer(IProducer producer) => Producer = producer;
    }
}