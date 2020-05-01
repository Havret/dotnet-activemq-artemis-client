# ActiveMQ. Net

Unofficial [ActiveMQ Artemis](https://activemq.apache.org/components/artemis/) . NET Client for . NET Core and . NET Framework

Apache ActiveMQ Artemis is an open-source project to build a multi-protocol, embeddable, very high performance, clustered, asynchronous messaging system. 

This lightweight . NET client library built on top of [Amqp. Net Lite](http://azure.github.io/amqpnetlite/) tries to fully leverage Apache ActiveMQ Artemis capabilities.

## API overview

The API interfaces and classes are defined in the `ActiveMQ.Net` namespace:

```csharp
using ActiveMQ.Net;
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

ActiveMQ. Net uses `IProducer` and `IAnonymousProducer` interfaces for sending messages. To to create an instance of `IProducer` you need to specify an address name and a routing type to which messages will be sent. 

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

ActiveMQ.Net uses `IConsumer` interface for receiving messages. `IConsumer` can be created as follows:

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

ActiveMQ.Net uses `Message` class to represent messages which may be transmitted. A `Message` can carry various types of payload and accompanying metadata.

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

Each message sent with this producer will automatically have priority set to `9` unless specified otherwise. The priority set explicitly on the message object takes the highest precedence.

```csharp
await producer.SendAsync(new Message("foo")
{
    Priority = 0 // takes precedence over priority specified on producer level
});
```

### Resources lifespan

Connections, producers, and consumers are meant to be long-lived objects. The underlying protocol is designed and optimized for long-running connections. That means that opening a new connection per operation, e.g. sending a message, is unnecessary and strongly discouraged as it will introduce a lot of network round trips and overhead. The same rule applies to all ActiveMQ. Net resources.
