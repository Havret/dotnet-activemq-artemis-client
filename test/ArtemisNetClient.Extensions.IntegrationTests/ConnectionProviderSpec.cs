using System;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace ActiveMQ.Artemis.Client.Extensions.AspNetCore.IntegrationTests;

public class ConnectionProviderSpec
{
    private readonly ITestOutputHelper _testOutputHelper;

    public ConnectionProviderSpec(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public async Task Should_resolve_connection_by_name()
    {
        var connectionName = "my-artemis-connection";
        await using var testFixture = await TestFixture.CreateAsync(_testOutputHelper, connectionName: connectionName);
        var connectionProvider = testFixture.Services.GetRequiredService<ConnectionProvider>();

        var connection = await connectionProvider.GetConnectionAsync(connectionName);
        
        Assert.NotNull(connection);
        Assert.True(connection.IsOpened);
    }
    
    [Fact]
    public async Task Should_resolve_the_same_instance_of_connection_when_called_multiple_times()
    {
        var connectionName = "my-artemis-connection";
        await using var testFixture = await TestFixture.CreateAsync(_testOutputHelper, connectionName: connectionName);
        var connectionProvider = testFixture.Services.GetRequiredService<ConnectionProvider>();

        var connection1 = await connectionProvider.GetConnectionAsync(connectionName);
        var connection2 = await connectionProvider.GetConnectionAsync(connectionName);
        
        Assert.Same(connection1, connection2);
    }

    [Fact]
    public async Task Should_throw_exception_on_attempt_to_resolve_connection_that_was_not_registered()
    {
        var connectionName = "genuine-connection-name";
        await using var testFixture = await TestFixture.CreateAsync(_testOutputHelper, connectionName: connectionName);
        var connectionProvider = testFixture.Services.GetRequiredService<ConnectionProvider>();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await connectionProvider.GetConnectionAsync("invalid-connection-name");
        });

        Assert.Equal("There is connection registered with name invalid-connection-name", exception.Message);
    }
}