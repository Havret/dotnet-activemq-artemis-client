---
id: getting-started
title: Getting started
sidebar_label: Getting Started
---

.NET ActiveMQ Artemis Client is a lightweight library built on top of [AmqpNetLite](http://azure.github.io/amqpnetlite/). The main goal of this project is to provide a simple API that allows fully leverage Apache ActiveMQ Artemis capabilities in .NET World.

## API overview

The API interfaces and classes are defined in the `ActiveMQ.Artemis.Client` namespace:

```csharp
using ActiveMQ.Artemis.Client;
```

The core API interfaces and classes are:

* `IConnection` : represents AMQP 1.0 connection
* `ConnectionFactory` : constructs IConnection instances
* `IConsumer` : represents a message consumer
* `IProducer` : represents a message producer attached to a specified *address*
* `IAnonymousProducer` : represents a message producer capable of sending messages to different *addresses*

## Creating a connection

Before any message can be sent or received, a connection to the broker endpoint has to be opened. The AMQP endpoint where the client connects is represented as an `Endpoint` object. The `Endpoint` object can be created using the factory method `Create` that accepts individual parameters specifying different parts of the Uri.

```csharp
Endpoint.Create(
    host: "localhost",
    port: 5672,
    user: "guest",
    password: "guest",
    scheme: Scheme.Amqp);
```

* `host` and the `port` parameters define TCP endpoint.
* `user` and `password` parameters determine if authentication (AMQP SASL)  should be performed after the transport is established. When `user` and `password` are absent, the library skips SASL negotiation altogether. 
* `scheme` parameter defines if a secure channel (TLS/SSL) should be established.

To open a connection to an ActiveMQ Artemis node, first instantiate a `ConnectionFactory` object. `ConnectionFactory` provides an asynchronous connection creation method that accepts `Endpoint` object.

The following snippet connects to an ActiveMQ Artemis node on `localhost` on port `5672` as a `guest` user using `guest` as a password via the insecure channel (AMQP).

```csharp
var connectionFactory = new ConnectionFactory();
var endpoint = Endpoint.Create("localhost", 5672, "guest", "guest");
var connection = await connectionFactory.CreateAsync(endpoint);
```

## Disconnecting

Closing connection, the same as opening, is a fully asynchronous operation.

To disconnect, simply call `DisposeAsync` on connection object:

```csharp
await connection.DisposeAsync();
```

:::important

Connections, producers, and consumers are meant to be long-lived objects. The underlying protocol is designed and optimized for long-running connections. That means that opening a new connection per operation, e.g. sending a message, is unnecessary and strongly discouraged as it will introduce a lot of network round trips and overhead. The same rule applies to all client resources.

:::

## Sending messages

The client uses `IProducer` and `IAnonymousProducer` interfaces for sending messages. To to create an instance of `IProducer` you need to specify an address name and a routing type to which messages will be sent.

```csharp
var producer = await connection.CreateProducerAsync("a1", RoutingType.Anycast);
```

All messages sent using this producer will be automatically routed to address `a1` using `Anycast` routing type:

```csharp
await producer.SendAsync(new Message("foo"));
```

To send messages to other addresses you can create more producers or use `IAnonymousProducer` interface. `IAnonymousProducer` is not connected to any particular address.

```csharp
var anonymousProducer = await connection.CreateAnonymousProducer();
```

Each time you want to send a message, you need to specify the address name and the routing type:

```csharp
await anonymousProducer.SendAsync("a2", RoutingType.Multicast, new Message("foo"));
```

## Receiving messages

The client uses `IConsumer` interface for receiving messages. `IConsumer` can be created as follows:

```csharp
var consumer = await connection.CreateConsumerAsync("a1", RoutingType.Anycast);
```

As soon as the subscription is set up, the messages will be delivered automatically as they arrive, and then buffered inside consumer object. The number of buffered messages can be controlled by `Consumer Credit` . In order to get a message, simply call `ReceiveAsync` on `IConsumer` .

```csharp
var message = await consumer.ReceiveAsync();
```

If there are no messages buffered `ReceiveAsync` will asynchronously wait and return as soon as the new message appears on the client.

This operation can potentially last an indefinite amount of time (if there are no messages), therefore `ReceiveAsync` accepts `CancellationToken` that can be used to communicate a request for cancellation.

```csharp
var cts = new CancellationTokenSource();
var message = await consumer.ReceiveAsync(cts.Token);
```

This may be particularly useful when you want to shut down your application.