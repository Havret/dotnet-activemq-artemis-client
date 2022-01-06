---
id: testing
title: Testing
sidebar_label: Testing
---

Automated testing is a crucial and intrinsic part of the software development cycle. .NET Client for ActiveMQ Artemis comes with a dedicated package `ArtemisNetClient.Testing` that will help you to test the messaging-dependent part of your application with ease in a controlled but realistic environment. 

## Installation

`ArtemisNetClient.Testing` is distributed via [NuGet](https://www.nuget.org/packages/ArtemisNetClient.Testing). You can add ArtemisNetClient.Testing NuGet package using dotnet CLI:

```
dotnet add package ArtemisNetClient.Testing
```

## TestKit

The central part of `ArtemisNetClient.Testing` is TestKit class. TestKit spins up an in-memory message broker that your system under test can connect to using AMQP protocol. It offers a simple API that allows you to send and receive messages to and from the application.

## Testing ASP.NET Core application  

Let's consider a very simple ASP.NET Core application that uses `ArtemisNetClient.Extensions.DependencyInjection` helper package to setup ArtemisNetClient and connect to the broker:

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddActiveMqHostedService();

var endpoint = Endpoint.Create(
    host: "localhost",
    port: 5672,
    "artemis",
    "artemis"
);
var activeMqBuilder = builder.Services.AddActiveMq(name: "my-artemis", endpoints: new[] {endpoint});
activeMqBuilder.AddConsumer(address: "foo", routingType: RoutingType.Anycast,
    handler: async (message, consumer, serviceProvider, cancellationToken) =>
    {
        var body = message.GetBody<string>();

        var bar = body + "-" + "bar";

        var producer = serviceProvider.GetRequiredService<MyProducer>();
        await producer.Publish(bar, cancellationToken);

        await consumer.AcceptAsync(message);
    });
activeMqBuilder.AddProducer<MyProducer>("bar", RoutingType.Multicast);


var app = builder.Build();

app.Run();

// this is required so we can use WebApplicationFactory to run the test server
public partial class Program { }
```

This application has a single consumer attached to `foo` address using anycast routing type. On each message the app performs some logic and as a final step publishes a new message to a `bar` address.

With `ArtemisNetClient.Testing` and `Microsoft.AspNetCore.Mvc.Testing` we can write the following integration test that will verify that our application works as expected:

```csharp
public class MyTests
{
    [Fact]
    public async Task Test()
    {
        // setup the test kit
        var endpoint = Endpoint.Create(
            host: "localhost",
            port: 5672,
            "artemis",
            "artemis"
        );
        using var testKit = new TestKit(endpoint);
        
        // setup the application
        await using var application = new WebApplicationFactory<Program>();

        // trigger the app to start
        application.CreateClient();

        // subscribe to the bar address
        using var subscription = testKit.Subscribe("bar");
        
        // send a message to the application
        await testKit.SendMessageAsync("foo", new Message("my-payload"));

        // wait for a message sent from the application
        var message = await subscription.ReceiveAsync();
        
        Assert.Equal("my-payload-bar", message.GetBody<string>());
    }
}
```
