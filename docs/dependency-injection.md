---
id: dependency-injection
title: Dependency Injection
sidebar_label: Dependency Injection
---

`ArtemisNetClient.Extensions.DependencyInjection` integrates ArtemisNetClient with `Microsoft.Extensions.DependencyInjection`. For ASP.NET Core applications, it is the easiest way to register long-lived connections, consumers, producers, and request-reply clients.

## Installation

For DI-only registration:

```bash
dotnet add package ArtemisNetClient.Extensions.DependencyInjection
```

For ASP.NET Core or generic hosted applications, add the hosting package as well:

```bash
dotnet add package ArtemisNetClient.Extensions.Hosting
```

The extension methods live in these namespaces:

```csharp
using ActiveMQ.Artemis.Client;
using ActiveMQ.Artemis.Client.Extensions.DependencyInjection;
using ActiveMQ.Artemis.Client.Extensions.Hosting;
```

## Minimal ASP.NET Core setup

`AddActiveMq` registers the connection and the ArtemisNetClient services in the DI container. `AddActiveMqHostedService` starts those registrations when the host starts.

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddActiveMqHostedService();

var endpoint = Endpoint.Create(
    host: "localhost",
    port: 5672,
    user: "artemis",
    password: "artemis");

var activeMq = builder.Services.AddActiveMq(
    name: "my-artemis",
    endpoints: new[] { endpoint });

activeMq.AddConsumer(
    address: "orders.incoming",
    routingType: RoutingType.Anycast,
    handler: async (message, consumer, serviceProvider, cancellationToken) =>
    {
        var publisher = serviceProvider.GetRequiredService<OrderEventsProducer>();
        var orderId = message.GetBody<string>();

        await publisher.PublishAccepted(orderId, cancellationToken);
        await consumer.AcceptAsync(message);
    });

activeMq.AddProducer<OrderEventsProducer>("orders.events", RoutingType.Multicast);

var app = builder.Build();
app.Run();

public sealed class OrderEventsProducer
{
    private readonly IProducer _producer;

    public OrderEventsProducer(IProducer producer) => _producer = producer;

    public Task PublishAccepted(string orderId, CancellationToken cancellationToken)
    {
        return _producer.SendAsync(new Message($"accepted:{orderId}"), cancellationToken);
    }
}
```

:::important

If you register ArtemisNetClient services with `AddActiveMq` but do not start them with `AddActiveMqHostedService`, your consumers and typed producers will not be started automatically.

This split is intentional. Hosted applications usually want `AddActiveMqHostedService`, while advanced scenarios can control startup manually through `IActiveMqClient`.

:::

## How the builder works

`AddActiveMq` returns an `IActiveMqBuilder`. Use that builder to compose the messaging setup for one logical connection:

- `ConfigureConnectionFactory` customizes `ConnectionFactory`.
- `ConfigureConnection` attaches runtime hooks to the created `IConnection`.
- `AddConsumer` registers message handlers.
- `AddProducer` registers typed producers backed by `IProducer`.
- `AddAnonymousProducer` registers typed producers backed by `IAnonymousProducer`.
- `AddRequestReplyClient` registers typed request-reply clients.
- `EnableQueueDeclaration` and `EnableAddressDeclaration` enable topology declaration on startup.

The `name` parameter identifies the logical connection registration. Use different names when you need multiple Artemis connections in the same application.

## Consumers

The simplest consumer attaches to an address and routing type:

```csharp
activeMq.AddConsumer(
    address: "orders.incoming",
    routingType: RoutingType.Anycast,
    handler: async (message, consumer, serviceProvider, cancellationToken) =>
    {
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Received order {OrderId}", message.GetBody<string>());

        await consumer.AcceptAsync(message);
    });
```

The handler signature is:

```csharp
Func<Message, IConsumer, IServiceProvider, CancellationToken, Task>
```

That gives you direct access to the incoming `Message`, the active `IConsumer`, the current `IServiceProvider`, and the shutdown `CancellationToken`.

Use the other overloads when you need more control:

- Add `ConsumerOptions` to configure credit, concurrent consumers, filters, or `NoLocal` behavior.
- Add a queue name when the consumer should bind to a specific queue.
- Add `QueueOptions` when queue declaration is enabled and queue settings need to be declared on startup.
- Use `AddSharedConsumer` or `AddSharedDurableConsumer` for shared subscription patterns.

## Typed producers

Typed producers keep your application code away from raw Artemis primitives while still reusing a long-lived underlying `IProducer`.

```csharp
activeMq.AddProducer<OrderEventsProducer>("orders.events", RoutingType.Multicast);

public sealed class OrderEventsProducer
{
    private readonly IProducer _producer;

    public OrderEventsProducer(IProducer producer) => _producer = producer;

    public Task PublishAccepted(string orderId, CancellationToken cancellationToken)
    {
        return _producer.SendAsync(new Message($"accepted:{orderId}"), cancellationToken);
    }
}
```

`AddProducer<TProducer>` registers `TProducer` as a typed wrapper. By default the wrapper type is transient, but you can choose `Singleton` or `Scoped` when that better matches your application.

Each typed wrapper type must be unique per registration. If you need two producers with the same public API, create two distinct wrapper classes.

## Anonymous producers

Use `AddAnonymousProducer<TProducer>` when the producer must publish to multiple addresses dynamically.

```csharp
activeMq.AddAnonymousProducer<ReplyProducer>();

public sealed class ReplyProducer
{
    private readonly IAnonymousProducer _producer;

    public ReplyProducer(IAnonymousProducer producer) => _producer = producer;

    public Task SendAsync(string address, Message message, CancellationToken cancellationToken)
    {
        return _producer.SendAsync(address, message, cancellationToken);
    }
}
```

## Request-reply

`AddRequestReplyClient<TClient>` registers a typed wrapper over `IRequestReplyClient`.

```csharp
activeMq.AddRequestReplyClient<BookstoreRequestClient>();

public sealed class BookstoreRequestClient
{
    private readonly IRequestReplyClient _client;

    public BookstoreRequestClient(IRequestReplyClient client) => _client = client;

    public Task<Message> SendAsync(string address, string payload, CancellationToken cancellationToken)
    {
        return _client.SendAsync(
            address,
            RoutingType.Anycast,
            new Message(payload),
            cancellationToken);
    }
}
```

On the response side, handle the request with a consumer and reply to `message.ReplyTo`. The response must preserve `message.CorrelationId`.

```csharp
activeMq.AddAnonymousProducer<ReplyProducer>();

activeMq.AddConsumer(
    address: "bookstore.requests",
    routingType: RoutingType.Anycast,
    handler: async (message, consumer, serviceProvider, cancellationToken) =>
    {
        var replyProducer = serviceProvider.GetRequiredService<ReplyProducer>();

        await replyProducer.SendAsync(
            message.ReplyTo,
            new Message("ok")
            {
                CorrelationId = message.CorrelationId
            },
            cancellationToken);

        await consumer.AcceptAsync(message);
    });
```

## Configuring the connection

Use `ConfigureConnectionFactory` for transport, logging, recovery, or message ID settings.

```csharp
activeMq.ConfigureConnectionFactory((serviceProvider, factory) =>
{
    factory.LoggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
    factory.AutomaticRecoveryEnabled = true;
});
```

Use `ConfigureConnection` for events on the created `IConnection`:

```csharp
activeMq.ConfigureConnection((_, connection) =>
{
    connection.ConnectionClosed += (_, args) =>
    {
        Console.WriteLine($"Connection closed. Error={args.Error}");
    };
});
```

## Declaring topology on startup

If the application should declare queues or addresses when it starts, enable topology declaration:

```csharp
activeMq
    .AddConsumer(
        address: "orders.incoming",
        routingType: RoutingType.Anycast,
        queue: "orders-service",
        handler: async (message, consumer, serviceProvider, cancellationToken) =>
        {
            await consumer.AcceptAsync(message);
        })
    .EnableQueueDeclaration()
    .EnableAddressDeclaration();
```

`QueueOptions` and `ConfigureTopology` let you refine the declared topology when startup needs to create or update broker objects.

## Multiple Artemis connections

Each `AddActiveMq(name, endpoints)` call registers one logical connection. Use different names when your application needs separate clusters or credentials.

Keep the registrations for one connection on the returned builder so the consumers, producers, topology configuration, and observers all stay attached to the intended connection.

## Manual startup and shutdown

Hosted applications typically use `AddActiveMqHostedService`, but the DI package also registers `IActiveMqClient` for manual lifecycle control.

That is useful when you need to start messaging later in the application lifecycle or coordinate startup with other infrastructure.

## Related examples

- [Minimal DI sample](https://github.com/Havret/dotnet-activemq-artemis-client/blob/master/samples/Testing/Application/Program.cs)
- [ASP.NET Core sample](https://github.com/Havret/dotnet-activemq-artemis-client/blob/master/samples/ArtemisNetClient.Examples.AspNetCore/Startup.cs)
- [Request-reply integration tests](https://github.com/Havret/dotnet-activemq-artemis-client/blob/master/test/ArtemisNetClient.Extensions.IntegrationTests/RequestReplyClientSpec.cs)