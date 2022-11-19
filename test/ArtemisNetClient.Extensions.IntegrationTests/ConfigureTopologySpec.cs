using System;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Extensions.DependencyInjection;
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
}