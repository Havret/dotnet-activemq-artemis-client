using System.Threading.Tasks;
using ActiveMQ.Artemis.Client;
using ActiveMQ.Artemis.Client.Testing;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace TestProject1;

public class MyTests
{
    [Fact]
    public async Task Test()
    {
        // setup the test kit
        var endpoint = Endpoint.Create(
            host: "localhost",
            port: 5699,
            "artemis",
            "artemis"
        );
        using var testKit = new TestKit(endpoint);
        
        // setup the application
        await using var application = new WebApplicationFactory<Program>();

        // trigger the app to start
        application.CreateClient();

        // subscribe to bar address
        using var subscription = testKit.Subscribe("bar");
        
        // send message to the application
        await testKit.SendMessageAsync("foo", new Message("my-payload"));

        // wait for a message send from the application
        var message = await subscription.ReceiveAsync();
        
        Assert.Equal("my-payload-bar", message.GetBody<string>());
    }
}