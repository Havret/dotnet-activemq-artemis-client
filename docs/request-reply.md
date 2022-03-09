---
id: request-reply
title: Request reply
sidebar_label: Request reply
---

*Request-reply* is one of the basic messaging patterns. From a high level, a request-reply scenario involves an application that sends a message (the request) and expects to receive a message in return (the reply).

## Implementing request-reply with ArtemisNetClient

Implementing this style of system architecture is really easy with ArtemisNetClient. 

### Request side

From the request side, all you need to do is create an instance of `IRequestReplyClient` interface. `IRequestReplyClient` is responsible for sending messages and listening to responses asynchronously.

```csharp
var connectionFactory = new ConnectionFactory();
var endpoint = Endpoint.Create("localhost", 5672, "guest", "guest");
var connection = await connectionFactory.CreateAsync(endpoint);
await using var requestReplyClient = await connection.CreateRequestReplyClientAsync();
var response = await requestReplyClient.SendAsync("my-address", RoutingType.Anycast, new Message("foo"), default);
var test = response.GetBody<string>(); // bar
```

### Response side

All the messages sent by `IRequestReplyClient` have a couple of important properties set:

- `ReplyTo` is the address where the reply message is expected to be delivered.
- `CorrelationId` is the unique identifier that is used by `IRequestReplyClient` to correlate requests with responses. You may have multiple outstanding requests, so it's crucial to set this property on the response message. 

```csharp
var connectionFactory = new ConnectionFactory();
var endpoint = Endpoint.Create("localhost", 5672, "guest", "guest");
var connection = await connectionFactory.CreateAsync(endpoint);
await using var consumer = await connection.CreateConsumerAsync("my-address", RoutingType.Anycast);
var request = await consumer.ReceiveAsync();
await using var producer = await connection1.CreateAnonymousProducerAsync();
await producer.SendAsync(request.ReplyTo, new Message("bar")
{
    CorrelationId = request.CorrelationId
});
await consumer.AcceptAsync(request);
```

##  Implementing request-reply with ArtemisNetClient.Extensions.DependencyInjection

### Request side

DependencyInjection package provides a simple way of registering `IRequestReplyClient` in a DI container. 

```csharp
public void ConfigureServices(IServiceCollection services)
{
    /*...*/ 
    var endpoints = new[] { Endpoint.Create(host: "localhost", port: 5672, "guest", "guest") };
    services.AddActiveMq("bookstore-cluster", endpoints)
            .AddRequestReplyClient<MyRequestReplyClient>();
    /*...*/
}
```

`MyRequestReplyClient` is your custom class that expects the `IRequestReplyClient` to be injected via the constructor. Once you have your custom class, you can either expose the `IRequestReplyClient` directly or encapsulate sending logic inside of it:

```csharp
public class MyRequestReplyClient
{
    private readonly IRequestReplyClient _requestReplyClient;
    public MyRequestReplyClient(IRequestReplyClient requestReplyClient)
    {
        _requestReplyClient = requestReplyClient;
    }

    public async Task<TResponse> SendAsync<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken)
    {
        var serialized = JsonSerializer.Serialize(request);
        var address = typeof(TRequest).Name;
        var msg = new Message(serialized);
        var response = await _requestReplyClient.SendAsync(address, msg, cancellationToken);
        return JsonSerializer.Deserialize<TResponse>(response.GetBody<string>());
    }
}
```

### Response side

TODO