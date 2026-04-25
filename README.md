# .NET Client for Apache Artemis

[![Build](https://github.com/Havret/dotnet-activemq-artemis-client/actions/workflows/build.yml/badge.svg)](https://github.com/Havret/dotnet-activemq-artemis-client/actions/workflows/build.yml)

Unofficial [Apache Artemis](https://artemis.apache.org/components/artemis/) .NET Client for .NET Core and .NET Framework.

Apache Artemis is an open-source project to build a multi-protocol, embeddable, very high performance, clustered, asynchronous messaging system.

This lightweight .NET client library built on top of [AmqpNetLite](http://azure.github.io/amqpnetlite/) tries to fully leverage Apache Artemis capabilities.

|NuGet|Status|
|------|-------------|
|ArtemisNetClient|[![NuGet](https://img.shields.io/nuget/vpre/ArtemisNetClient.svg)](https://www.nuget.org/packages/ArtemisNetClient/)
|ArtemisNetClient.Extensions.DependencyInjection|[![NuGet](https://img.shields.io/nuget/vpre/ArtemisNetClient.Extensions.DependencyInjection.svg)](https://www.nuget.org/packages/ArtemisNetClient.Extensions.DependencyInjection/)
|ArtemisNetClient.Extensions.Hosting |[![NuGet](https://img.shields.io/nuget/vpre/ArtemisNetClient.Extensions.Hosting.svg)](https://www.nuget.org/packages/ArtemisNetClient.Extensions.Hosting/)
|ArtemisNetClient.Extensions.App.Metrics |[![NuGet](https://img.shields.io/nuget/vpre/ArtemisNetClient.Extensions.App.Metrics.svg)](https://www.nuget.org/packages/ArtemisNetClient.Extensions.App.Metrics/)
|ArtemisNetClient.Extensions.CloudEvents|[![NuGet](https://img.shields.io/nuget/vpre/ArtemisNetClient.Extensions.CloudEvents.svg)](https://www.nuget.org/packages/ArtemisNetClient.Extensions.CloudEvents/)
|ArtemisNetClient.Extensions.LeaderElection |
|ArtemisNetClient.Testing|[![NuGet](https://img.shields.io/nuget/vpre/ArtemisNetClient.Testing.svg)](https://www.nuget.org/packages/ArtemisNetClient.Testing/)

## Commercial support

For commercial support, contact<br/>
![#](https://raw.githubusercontent.com/Havret/dotnet-activemq-artemis-client/master/website/static/img/email.png)

## Compatibility

The library is tested against **Artemis 2.30.0** ([Jul 26th, 2023](https://github.com/Havret/dotnet-activemq-artemis-client/issues/453)). Most of the features should work with older versions of the broker, but **Topology Management** uses API surface that was introduced in **Artemis 2.14.0**.

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

If your application already uses `Microsoft.Extensions.DependencyInjection` or ASP.NET Core, start with the dependency injection package instead of wiring the core client manually:

```
dotnet add package ArtemisNetClient.Extensions.DependencyInjection
dotnet add package ArtemisNetClient.Extensions.Hosting
```

See the [Dependency Injection guide](docs/dependency-injection.md) for the hosted setup, typed producers, consumers, and request-reply registration.

## Documentation

Detailed documentation is available on [the project website](https://havret.github.io/dotnet-activemq-artemis-client/).

- [Getting Started](docs/getting-started.md)
- [Dependency Injection](docs/dependency-injection.md)
- [Request Reply](docs/request-reply.md)
- [Testing](docs/testing.md)

## Online resources

- [Messaging with ActiveMQ Artemis and ASP.NET Core](https://havret.io/activemq-artemis-net-core) (January 31, 2021)
- [Run ActiveMQ Artemis in Docker for local development](https://blog.jeroenmaes.eu/2021/12/run-activemq-artemis-in-docker-for-local-development/) (December 15, 2021)
- [ActiveMQ Artemis address model explained with examples in .NET](https://havret.io/activemq-artemis-address-model) (April 19, 2022)

## Running the tests
The tests are grouped in two categories:
- Unit Tests that don't require any infrastructure to run
- Integration Tests that require Apache Artemis 

You can run the unit tests suite with the following command:

```
dotnet test --filter "FullyQualifiedName!~IntegrationTests"
```

Integration tests can be run with an Apache Artemis server hosted in a Docker container. 

### Spinning up the necessary infrastructure
This assumes docker and docker-compose are properly setup.

1. Go to `/test/artemis` directory
2. Run the following command: `docker-compose up -V -d`

Having the broker up and running you can execute the integration test suite:

```
dotnet test --filter "FullyQualifiedName~IntegrationTests"
```

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
There are several extensions available that make integration of .NET Client for Apache Artemis with ASP.NET Core based projects seamless.

- `ArtemisNetClient.Extensions.DependencyInjection`: registers connections, consumers, producers, request-reply clients, and topology configuration in `IServiceCollection`. Start here for ASP.NET Core. See [Dependency Injection](docs/dependency-injection.md).
- `ArtemisNetClient.Extensions.Hosting`: starts Artemis registrations through `IHostedService`. Used together with the DI package in hosted apps.
- `ArtemisNetClient.Extensions.HealthChecks`: exposes Artemis connectivity through ASP.NET Core health checks.
- `ArtemisNetClient.Extensions.LeaderElection`: adds distributed leader election on top of the DI and hosting integration.

Examples:

- [Minimal DI sample](samples/Testing/Application/Program.cs)
- [ASP.NET Core sample](samples/ArtemisNetClient.Examples.AspNetCore/Startup.cs)
- [Leader election sample](samples/ArtemisNetClient.Examples.LeaderElection/Startup.cs)

## ⚠️ Breaking Change in 3.0.0

Version **3.0.0** introduces a **breaking change** to the `IConsumer` API.

- The `Reject(Message message, bool undeliverableHere = false)` method has been **replaced** with two explicit methods:
  - `Modify(Message message, bool deliveryFailed, bool undeliverableHere)`
  - `Reject(Message message)`

📄 **Please see the [3.0.0 Release Notes](https://github.com/Havret/dotnet-activemq-artemis-client/releases/tag/v3.0.0) for detailed migration instructions.**

## License

This project uses [MIT licence](https://github.com/Havret/dotnet-activemq-artemis-client/blob/master/LICENSE). Long story short - you are more than welcome to use it anywhere you like, completely free of charge and without oppressive obligations.
