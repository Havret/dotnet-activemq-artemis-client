using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace ActiveMQ.Artemis.Client.Extensions.AspNetCore.IntegrationTests;

public class RequestReplyClientLifetimeSpec
{
    private readonly ITestOutputHelper _testOutputHelper;

    public RequestReplyClientLifetimeSpec(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public async Task Should_register_request_reply_client_with_transient_service_lifetime_by_default()
    {
        await using var testFixture = await TestFixture.CreateAsync(_testOutputHelper, builder =>
        {
            builder.AddRequestReplyClient<TestRequestReplyClient>();
        });

        var requestReplyClient1 = testFixture.Services.GetRequiredService<TestRequestReplyClient>();
        var requestReplyClient2 = testFixture.Services.GetRequiredService<TestRequestReplyClient>();

        Assert.NotEqual(requestReplyClient1, requestReplyClient2);
        Assert.Equal(requestReplyClient1.Client, requestReplyClient2.Client);
    }
    
    [Fact]
    public async Task Should_register_request_reply_client_with_singleton_service_lifetime()
    {
        await using var testFixture = await TestFixture.CreateAsync(_testOutputHelper, builder =>
        {
            builder.AddRequestReplyClient<TestRequestReplyClient>(ServiceLifetime.Singleton);
        });

        var requestReplyClient1 = testFixture.Services.GetRequiredService<TestRequestReplyClient>();
        var requestReplyClient2 = testFixture.Services.GetRequiredService<TestRequestReplyClient>();

        Assert.Equal(requestReplyClient1, requestReplyClient2);
        Assert.Equal(requestReplyClient1.Client, requestReplyClient2.Client);
    }
    
    [Fact]
    public async Task Should_register_request_reply_client_with_Scoped_service_lifetime()
    {
        await using var testFixture = await TestFixture.CreateAsync(_testOutputHelper, builder =>
        {
            builder.AddRequestReplyClient<TestRequestReplyClient>(ServiceLifetime.Scoped);
        });

        using var scope = testFixture.Services.CreateScope();
        var requestReplyClient1Scope1 = scope.ServiceProvider.GetRequiredService<TestRequestReplyClient>();
        var requestReplyClient2Scope1 = scope.ServiceProvider.GetRequiredService<TestRequestReplyClient>();

        using var scope2 = testFixture.Services.CreateScope();
        var requestReplyClient1Scope2 = scope2.ServiceProvider.GetService<TestRequestReplyClient>();

        Assert.Equal(requestReplyClient1Scope1, requestReplyClient2Scope1);
        Assert.Equal(requestReplyClient1Scope1.Client, requestReplyClient2Scope1.Client);
        Assert.NotEqual(requestReplyClient1Scope1, requestReplyClient1Scope2);
        Assert.Equal(requestReplyClient1Scope1.Client, requestReplyClient1Scope2.Client);
    }
    
    private class TestRequestReplyClient
    {
        public IRequestReplyClient Client { get; }
        public TestRequestReplyClient(IRequestReplyClient client) => Client = client;
    }
}