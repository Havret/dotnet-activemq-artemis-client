# .NET Client for ActiveMQ Artemis

[![Build](https://github.com/Havret/dotnet-activemq-artemis-client/actions/workflows/build.yml/badge.svg)](https://github.com/Havret/dotnet-activemq-artemis-client/actions/workflows/build.yml)

Unofficial [ActiveMQ Artemis](https://activemq.apache.org/components/artemis/) .NET Client for .NET Core and .NET Framework.

Apache ActiveMQ Artemis is an open-source project to build a multi-protocol, embeddable, very high performance, clustered, asynchronous messaging system.

This lightweight .NET client library built on top of [AmqpNetLite](http://azure.github.io/amqpnetlite/) tries to fully leverage Apache ActiveMQ Artemis capabilities.

|NuGet|Status|
|------|-------------|
|ArtemisNetClient|[![NuGet](https://img.shields.io/nuget/vpre/ArtemisNetClient.svg)](https://www.nuget.org/packages/ArtemisNetClient/)
|ArtemisNetClient.Extensions.DependencyInjection|[![NuGet](https://img.shields.io/nuget/vpre/ArtemisNetClient.Extensions.DependencyInjection.svg)](https://www.nuget.org/packages/ArtemisNetClient.Extensions.DependencyInjection/)
|ArtemisNetClient.Extensions.Hosting |[![NuGet](https://img.shields.io/nuget/vpre/ArtemisNetClient.Extensions.Hosting.svg)](https://www.nuget.org/packages/ArtemisNetClient.Extensions.Hosting/)
|ArtemisNetClient.Extensions.App.Metrics |[![NuGet](https://img.shields.io/nuget/vpre/ArtemisNetClient.Extensions.App.Metrics.svg)](https://www.nuget.org/packages/ArtemisNetClient.Extensions.App.Metrics/)
|ArtemisNetClient.Extensions.LeaderElection |
|ArtemisNetClient.Testing|[![NuGet](https://img.shields.io/nuget/vpre/ArtemisNetClient.Testing.svg)](https://www.nuget.org/packages/ArtemisNetClient.Testing/)

## Commercial support

For commercial support, contact<br/>
![#](https://raw.githubusercontent.com/Havret/dotnet-activemq-artemis-client/master/website/static/img/email.png)

## Compatibility

The library is tested against **ActiveMQ Artemis 2.23.1** ([Jun 21st, 2022](https://github.com/Havret/dotnet-activemq-artemis-client/issues/356)). Most of the features should work with older versions of the broker, but **Topology Management** uses API surface that was introduced in **ActiveMQ Artemis 2.14.0**.

## Quickstart

Add ArtemisNetClient NuGet package to your project using dotnet CLI:

```
dotnet add package ArtemisNetClient
```

The API interfaces and classes are defined in the `ActiveMQ.Artemis.Client` namespace:

```
using ActiveMQ.Artemis.Client;
```

Before any message can be sent or received, a connection to the broker endpoint has to be opened. Create a connection using `ConnectionFactory` object.

```
var connectionFactory = new ConnectionFactory();
var endpoint = Endpoint.Create("localhost", 5672, "guest", "guest");
var connection = await connectionFactory.CreateAsync(endpoint);
```

In order to send a message you will need a message producer:

```
var producer = await connection.CreateProducerAsync("a1", RoutingType.Anycast);
await producer.SendAsync(new Message("foo"));
```

To receive a message you have to create a message consumer:

```
var consumer = await connection.CreateConsumerAsync("a1", RoutingType.Anycast);
var message = await consumer.ReceiveAsync();
```

## Documentation

Detailed documentation is available on [the project website](https://havret.github.io/dotnet-activemq-artemis-client/).

## Online resources

- [Messaging with ActiveMQ Artemis and ASP.NET Core](https://havret.io/activemq-artemis-net-core) (January 31, 2021)
- [Run ActiveMQ Artemis in Docker for local development](https://blog.jeroenmaes.eu/2021/12/run-activemq-artemis-in-docker-for-local-development/) (December 15, 2021)
- [ActiveMQ Artemis address model explained with examples in .NET](https://havret.io/activemq-artemis-address-model) (April 19, 2022)

## Features

The following table shows what features are currently supported.

|Feature|Status|Comments|
|:-:|:-:|:-|
|Connection|✔|Allows to asynchronously create producers and consumers.|
|Session|✔|Sessions are created transparently for each link (producer or consumer).|
|Message Consumer|✔|`ReceiveAsync` available. Message consumer can by attached to pre-existing queue or can be instructed to create address and queue dynamically.|
|Message Producer|✔|`SendAsync` for *durable* messages and non-blocking fire and forget `Send` for *nondurable* messages.|
|Anonymous Message Producer|✔||
|Message Payload|✔|All primitive types, `System.Guid`, `System.DateTime`, `byte[]` and `Amqp.Types.List`.|
|Message Durability|✔||
|Message Priority|✔||
|Message Creation Time|✔|All messages have `CreationTime` set, but it can be disabled via producer configuration.|
|Message Grouping|✔||
|Message Expiry|✔||
|Message Acknowledgement|✔|Messages can be either *accepted* or *rejected*.|
|Scheduled Messages|✔|Message delivery can be scheduled either by `ScheduledDeliveryTime` or `ScheduledDeliveryDelay` property.
|Filter Expressions|✔||
|NoLocal filter|✔|Allows your consumers to subscribe *exclusively* to messages sent by producers created by other connections.|
|Consumer Credit|✔||
|Auto-recovery|✔|4 built-in recovery policies `ConstantBackoff`, `LinearBackoff`, `ExponentialBackoff`, `DecorrelatedJitterBackoff` and the option to implement your own recovery policy via `IRecoveryPolicy` interface.|
|Address Model|✔|Advanced routing strategies use FQQN, thus require queues to be pre-configured using `TopologyManager` or by editing `broker.xml` file. It is also possible to create shared, non-durable (volatile) subscriptions and shared durable subscriptions dynamically using `IConsumer` API.|
|Topology Management|✔|`TopologyManager` supports: getting all address names, getting all queue names, address creation, queue creation, address declaration, queue declaration, address removal, queue removal.|
|Logging|✔|Uses [Microsoft.Extensions.Logging.Abstractions](https://www.nuget.org/packages/Microsoft.Extensions.Logging.Abstractions/).|
|Queue Browser|❌||
|Transactions|✔||
|Consumer Priority|❌||
|[Test Kit](https://havret.github.io/dotnet-activemq-artemis-client/docs/testing)|✔|ArtemisNetClient comes with a dedicated package `ArtemisNetClient.Testing` that will help you to test the messaging-dependent part of your application with ease in a controlled but realistic environment.|
|[Request-reply](https://havret.github.io/dotnet-activemq-artemis-client/docs/request-reply)|✔|Request-reply can be implemented with `IRequestReplyClient`.|

## Extensions
There are several extensions available that make integration of .NET Client for ActiveMQ Artemis with ASP.NET Core based projects seamless.

## License

This project uses [MIT licence](https://github.com/Havret/dotnet-activemq-artemis-client/blob/master/LICENSE). Long story short - you are more than welcome to use it anywhere you like, completely free of charge and without oppressive obligations.
