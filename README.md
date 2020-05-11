# .NET Client for ActiveMQ Artemis

Unofficial [ActiveMQ Artemis](https://activemq.apache.org/components/artemis/) .NET Client for .NET Core and .NET Framework.

Apache ActiveMQ Artemis is an open-source project to build a multi-protocol, embeddable, very high performance, clustered, asynchronous messaging system.

This lightweight .NET client library built on top of [AmqpNetLite](http://azure.github.io/amqpnetlite/) tries to fully leverage Apache ActiveMQ Artemis capabilities.

## Features

The following table shows what features are currently supported.

|Feature|Status|Comments|
|:-:|:-:|:-|
|Connection|✔|Allows to asynchronously create producers and consumers.|
|Session|✔|Sessions are created transparently for each link (producer or consumer).|
|Message Consumer|✔|`ReceiveAsync` available.|
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
|NoLocal filter|✔|Allows your consumers to subscribe to messages sent by producers created by other connections.|
|Consumer Credit|✔||
|Auto-recovery|✔|3 built-in recovery policies `ConstantBackoff`, `LinearBackoff`, `ExponentialBackoff`, and the option to implement your own recovery policy via `IRecoveryPolicy` interface.|
|Address Model|✔|Advanced routing strategies use FQQN, thus require queues to be pre-configured.|
|Logging|✔|Uses [Microsoft.Extensions.Logging.Abstractions](https://www.nuget.org/packages/Microsoft.Extensions.Logging.Abstractions/).|
|Queue Browser|❌||
|Transactions|✔||
|Consumer Priority|❌||

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

## Usage

Before any message can be sent or received, a connection to the broker endpoint has to be opened. The AMQP endpoint where the client connects is represented as an `Endpoint` object. The `Endpoint` object can be created using the factory method `Create` that accepts individual parameters specifying different parts of the Uri.

```csharp
Endpoint.Create(host: "localhost", port: 5672, user: "guest", password: "guest", scheme: Scheme.Amqp)
```

* `host` and the `port` parameters define TCP endpoint.
* `user` and `password` parameters determine if authentication (AMQP SASL)  should be performed after the transport is established. When `user` and `password` are absent, the library skips SASL negotiation altogether. 
* `scheme` parameter defines if a secure channel (TLS/SSL) should be established.

### Creating a connection

To open a connection to an ActiveMQ Artemis node, first instantiate a `ConnectionFactory` object. `ConnectionFactory` provides an asynchronous connection creation method that accepts `Endpoint` object.

The following snippet connects to an ActiveMQ Artemis node on `localhost` on port `5672` as a `guest` user using `guest` as a password via the insecure channel (AMQP).

```csharp
var connectionFactory = new ConnectionFactory();
var endpoint = Endpoint.Create("localhost", 5672, "guest", "guest");
var connection = await connectionFactory.CreateAsync(endpoint);
```

### Disconnecting from ActiveMQ Artemis

Closing connection, the same as opening, is a fully asynchronous operation.

To disconnect, simply call `DisposeAsync` on connection object:

```csharp
await connection.DisposeAsync();
```

### Sending messages

The client uses `IProducer` and `IAnonymousProducer` interfaces for sending messages. To to create an instance of `IProducer` you need to specify an address name and a routing type to which messages will be sent.

```csharp
var producer = await connection.CreateProducerAsync("a1", AddressRoutingType.Anycast);
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
await anonymousProducer.SendAsync("a2", AddressRoutingType.Multicast, new Message("foo"));
```

### Receiving messages

The client uses `IConsumer` interface for receiving messages. `IConsumer` can be created as follows:

```csharp
var consumer = await connection.CreateConsumerAsync("a1", QueueRoutingType.Anycast);
```

As soon as the subscription is set up, the messages will be delivered automatically as they arrive, and then buffered inside consumer object. The number of buffered messages can be controlled by `Consumer Credit` . In order to get a message, simply call `ReceiveAsync` on `IConsumer` .

```csharp
var message = await consumer.ReceiveAsync();
```

If there are no messages buffered `ReceiveAsync` will asynchronously wait and returns as soon as the new message appears on the client.

This operation can potentially last an indefinite amount of time (if there are no messages), therefore `ReceiveAsync` accepts `CancellationToken` that can be used to communicate a request for cancellation.

```csharp
var cts = new CancellationTokenSource();
var message = await consumer.ReceiveAsync(cts.Token);
```

This may be particularly useful when you want to shut down your application.

### Message payload

The client uses `Message` class to represent messages which may be transmitted. A `Message` can carry various types of payload and accompanying metadata.

A new message can be created as follows:

```csharp
var message = new Message("foo");
```

The `Message` constructor accepts a single parameter of type object. It's the message body. Although body argument is very generic, only certain types are considered as a valid payload:

* `string`
* `char`
* `byte`
* `sbyte`
* `short`
* `ushort`
* `int`
* `uint`
* `long`
* `ulong`
* `float`
* `System.Guid`
* `System.DateTime`
* `byte[]`
* `Amqp.Types.List`

An attempt to pass an argument out of this list will result in `ArgumentOutOfRangeException`. Passing `null` is not acceptable either and cause `ArgumentNullException`.

In order to get the message payload call `GetBody` and specify the expected type of the body section:

```csharp
var body = message.GetBody<T>();
```

If `T` matches the type of the payload, the value will be returned, otherwise, you will get `default(T)`.

### Message properties

#### DurabilityMode

ActiveMQ Artemis supports two types of durability modes for messages: durable and nondurable. By default each message sent by the client using `SendAsync` method is durable. That means that the broker actually has to persist the message on the disk before the confirmation frame (ack) will be sent back to the client. The confirmation frame is what we can *await* for.

```csharp
await producer.SendAsync(new Message("foo")
```

When `SendAsync` completes without errors we may expect that the message will be delivered with *at least once* semantics. Durable messages incur more overhead due to the need to store the message, and value reliability over performance.

Setting message durability mode to `Nondurable` instructs the broker not to persist the message. By default each message sent by the client using `Send` method is nondurable. `Send` is non-blocking both in terms of packets transmission (it uses [Socket.SendAsync](https://docs.microsoft.com/en-us/dotnet/api/system.net.sockets.socket.sendasync) method in the fire and forget manner) and in terms of waiting for confirmation from the broker (it doesn't). As a result, *at most once* is all that you can expect in terms of message delivery guarantees from this combination. Nondurable messages incur less overhead and value performance over reliability.

Default message durability settings may be overridden via producer configuration:

```csharp
var producer = await connection.CreateProducerAsync(new ProducerConfiguration
{
    Address = "a1",
    RoutingType = AddressRoutingType.Anycast,
    MessageDurabilityMode = DurabilityMode.Nondurable
});
```

And for each message individually:

```csharp
await producer.SendAsync(new Message("foo")
{
    DurabilityMode = DurabilityMode.Nondurable // takes precedence over durability mode specified on producer level
});
```

#### Priority

This property defines the level of importance of a message. ActiveMQ Artemis uses it to prioritize message delivery. Messages with higher priority will be delivered before messages with lower priority. Messages with the same priority level should be delivered according to the order they were sent with. There are 10 levels of message priority, ranging from 0 (the lowest) to 9 (the highest). If no message priority is set on the client (Priority set to `null`), the message will be treated as if it was assigned a normal priority (4).

Default message priority can be overridden on message producer level:

```csharp
var producer = await connection.CreateProducerAsync(new ProducerConfiguration
{
    Address = "a1",
    RoutingType = AddressRoutingType.Anycast,
    MessagePriority = 9
});
```

Each message sent with this producer will automatically have priority set to `9` unless specified otherwise. The priority set explicitly on the message object takes the precedence.

```csharp
await producer.SendAsync(new Message("foo")
{
    Priority = 0 // takes precedence over priority specified on producer level
});
```

### Resources lifespan

Connections, producers, and consumers are meant to be long-lived objects. The underlying protocol is designed and optimized for long-running connections. That means that opening a new connection per operation, e.g. sending a message, is unnecessary and strongly discouraged as it will introduce a lot of network round trips and overhead. The same rule applies to all client resources.

### Automatic Recovery From Network Failures

A network connection between clients and ActiveMQ Artemis nodes can fail. The client library supports the automatic recovery of connections, producers, and consumers. Automatic recovery is enabled by default.

Automatic recovery is controlled by `IRecoveryPolicy` interface:

```csharp
public interface IRecoveryPolicy
{
    int RetryCount { get; }
    TimeSpan GetDelay(int attempt);
}
```

This interface defines how long the delay will be between subsequent recovery attempts if recovery fails due to an exception (e.g. ActiveMQ Artemis node is still not reachable), and how many recovery attempts will be made before terminal exception will be signaled.

You can subscribe to this using `IConnection.ConnectionRecoveryError` event:

```csharp
var connectionFactory = new ConnectionFactory();
var connection = await connectionFactory.CreateAsync(endpoint);
connection.ConnectionRecoveryError += (sender, eventArgs) =>
{
    // react accordingly
};
```

When the client library successfully reestablishes the connection, `IConnection.ConnectionRecovered` event is triggered.

To retry indefinite amount of times `RetryCount` should return `int.MaxValue`.

If the initial connection to an ActiveMQ Artemis node fails, automatic connection recovery will kick in as well. It may be problematic is some scenarios, as `ConnectionFactory.CreateAsync` won't signal any issues until the recovery policy gives up. If your recovery policy is configured to try to recover forever it may even never happen. That means you would be asynchronously waiting for the result of `CreateAsync` forever. To address this issue you can pass `CancellationToken` to `CreateAsync`. This allows you to arbitrarily break the operation at any point.

There are 3 built-in recovery policies that are available via `RecoveryPolicyFactory`:

#### Constant Backoff Recovery Policy

This policy instructs the connection recovery mechanism to wait a constant amount of time between recovery attempts.

The following defines a policy that will retry 5 times and wait 1s between each recovery attempt.

```csharp
var constantBackoff = RecoveryPolicyFactory.ConstantBackoff(delay: TimeSpan.FromSeconds(1), retryCount: 5);
```

#### Linear Backoff Recovery Policy

This policy instructs the connection recovery mechanism to wait increasingly longer times between recovery attempts.

The following defines a policy with a linear retry delay of 100, 200, 300, 400, 500ms.

```csharp
var linearBackoff = RecoveryPolicyFactory.LinearBackoff(initialDelay: TimeSpan.FromMilliseconds(100), retryCount: 5);
```  

The default linear factor is 1.0. However, it can be changed:

```csharp
var linearBackoff = RecoveryPolicyFactory.LinearBackoff(initialDelay: TimeSpan.FromMilliseconds(100), retryCount: 5, factor: 2);
```

This will create an increasing retry delay of 100, 300, 500, 700, 900ms.

Note, the linear factor must be greater than or equal to zero. A factor of zero will return equivalent retry delays to the `ConstantBackoffRecoveryPolicy`.

When the infinite number of retires is used, it may be useful to specify a maximum delay:

```csharp
var linearBackoff = RecoveryPolicyFactory.LinearBackoff(initialDelay: TimeSpan.FromMilliseconds(100), maxDelay: TimeSpan.FromSeconds(15));
```

#### Exponential Backoff Recovery Policy

This policy instructs the connection recovery mechanism to use the exponential function to calculate subsequent delays between recovery attempts. The delay duration is specified as `initialDelay x 2^attempt`. Because of the exponential nature (potential for rapidly increasing delay times), it is recommended to use this policy with a low starting delay, and explicitly setting maximum delay.

```csharp
var exponentialBackoff = RecoveryPolicyFactory.ExponentialBackoff(initialDelay: TimeSpan.FromMilliseconds(100), retryCount: 5);
```

This will create an exponentially increasing retry delay of 100, 200, 400, 800, 1600ms.

The default exponential growth factor is 2.0. However, you can provide our own.

```csharp
var exponentialBackoff = RecoveryPolicyFactory.ExponentialBackoff(initialDelay: TimeSpan.FromMilliseconds(100), retryCount: 5, factor: 4.0);
```

The upper for this retry with a growth factor of four is 25,600ms.

Note, the growth factor must be greater than or equal to one. A factor of one will return equivalent retry delays to the `ConstantBackoffRecoveryPolicy`.

#### Recover from first failure fast

All build-in recovery policies include an option to recover after the first failure immediately. You can enable this by passing in `fastFirst: true` to any of the policy factory methods.

```csharp
var recoveryPolicy = RecoveryPolicyFactory.ExponentialBackoff(initialDelay: TimeSpan.FromMilliseconds(100), retryCount: 5, factor: 4.0, fastFirst: true);
```

Note, the first recovery attempt will happen immediately and it will count against your retry count. That is, this will still try to recover five times but the first recovery attempt will happen immediately after connection to the broker is lost.

The logic behind a fast first recovery strategy is that failure may just have been a transient blip rather than reflecting a deeper underlying issue that for instance results in a broker failover.

#### Disabling Automatic Recovery

To disable automatic recovery, set `ConnectionFactory.AutomaticRecoveryEnabled` to false:

```csharp
var connectionFactory = new ConnectionFactory();
connectionFactory.AutomaticRecoveryEnabled = false;
// connection that will not recover automatically

var connection = await connectionFactory.CreateAsync(endpoint);
```

#### Failover

To provide high availability your typical ActiveMQ Artemis cluster configuration should contain at least 2 nodes: a master and a slave. For most of the time, only the master node is operational and it handles all of the requests. When the master goes down, however, failover occurs and the slave node becomes active.

To handle this scenario with the client library you need to use `ConnectionFactory.CreateAsync` overload that accepts `IEnumerable<Endpoint>`. This way when the connection to the first node is lost, the auto-recovery mechanism will try to reconnect to the second node. The endpoints are selected in a round-robin fashion using the original sequence with which the connection was created.

```csharp
var masterEndpoint = Endpoint.Create(host: "master", port: 5672, user: "guest", password: "guest", scheme: Scheme.Amqp);
var slaveEndpoint = Endpoint.Create(host: "slave", port: 5672, user: "guest", password: "guest", scheme: Scheme.Amqp);
var connectionFactory = new ConnectionFactory();
var connection = await connectionFactory.CreateAsync(new[] { masterEndpoint, slaveEndpoint });
```