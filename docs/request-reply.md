---
id: request-reply
title: Request Reply
sidebar_label: Request Reply
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

The dependency injection package exposes request-reply through `AddRequestReplyClient<TClient>`. For ASP.NET Core and hosted applications, pair it with `AddActiveMqHostedService()` so the underlying client is started automatically.

For the full registration flow, see [Dependency Injection](dependency-injection.md).

### Request side

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddActiveMqHostedService();

var endpoint = Endpoint.Create("localhost", 5672, "guest", "guest");
var activeMq = builder.Services.AddActiveMq("bookstore-cluster", new[] { endpoint });
activeMq.AddRequestReplyClient<BookstoreRequestClient>();

public sealed class BookstoreRequestClient
{
    private readonly IRequestReplyClient _requestReplyClient;

    public BookstoreRequestClient(IRequestReplyClient requestReplyClient)
    {
        _requestReplyClient = requestReplyClient;
    }

    public Task<Message> SendAsync(string payload, CancellationToken cancellationToken)
    {
        return _requestReplyClient.SendAsync(
            "bookstore.requests",
            RoutingType.Anycast,
            new Message(payload),
            cancellationToken);
    }
}
```

### Response side

On the response side, use a consumer and reply to `message.ReplyTo`. The reply must copy the original `CorrelationId`.

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddActiveMqHostedService();

var endpoint = Endpoint.Create("localhost", 5672, "guest", "guest");
var activeMq = builder.Services.AddActiveMq("bookstore-cluster", new[] { endpoint });

activeMq.AddAnonymousProducer<ReplyProducer>();
activeMq.AddConsumer(
    address: "bookstore.requests",
    routingType: RoutingType.Anycast,
    handler: async (message, consumer, serviceProvider, cancellationToken) =>
    {
        var replyProducer = serviceProvider.GetRequiredService<ReplyProducer>();

        await replyProducer.SendAsync(
            message.ReplyTo,
            new Message("accepted")
            {
                CorrelationId = message.CorrelationId
            },
            cancellationToken);

        await consumer.AcceptAsync(message);
    });

public sealed class ReplyProducer
{
    private readonly IAnonymousProducer _producer;

    public ReplyProducer(IAnonymousProducer producer)
    {
        _producer = producer;
    }

    public Task SendAsync(string address, Message message, CancellationToken cancellationToken)
    {
        return _producer.SendAsync(address, message, cancellationToken);
    }
}
```