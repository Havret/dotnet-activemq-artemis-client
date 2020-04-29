# ActiveMQ.Net
Unofficial [ActiveMQ Artemis](https://activemq.apache.org/components/artemis/) . NET Client for . NET Core and . NET Framework

Apache ActiveMQ Artemis is an open source project to build a multi-protocol, embeddable, very high performance, clustered, asynchronous messaging system. 

This lightweight .NET client library built on top of [Amqp.Net Lite](http://azure.github.io/amqpnetlite/) tries to fully leverage Apache ActiveMQ Artemis capabilities.

## API overview

The API interfaces and classes are defined in the `ActiveMQ.Net` namespace:

``` 
using ActiveMQ.Net;
```

The core API interfaces and classes are:

* `IConnection` : represents AMQP 1.0 connection
* `ConnectionFactory` : constructs IConnection instances
* `IConsumer` : represents a message consumer
* `IProducer` : represents a message producer attached to a specified *address*
* `IAnonymousProducer` : represents a message producer capable of sending messages to different *addresses*

## Usage

Before any message can be sent or received, a connection to the broker endpoint has to be opened. The AMQP endpoint where the client connects is represented as an `Endpoint` object. The `Endpoint` object can be created using factory method `Create` that accepts individual parameters specifying different parts of the Uri.

``` 
Endpoint.Create(host: "localhost", port: 5672, user: "guest", password: "guest", scheme: Scheme.Amqp)
```

* `host` and the `port` parameters define TCP endpoint.
* `user` and `password` parameters determine if authentication (AMQP SASL)  should be performed after the transport is established. When `user` and `password` are absent, the library skips SASL negotiation altogether. 
* `scheme` parameter defines if a secure channel (TLS/SSL) should be established.

### Creating a connection

To open a connection to an ActiveMQ Artemis node, first instantiate a `ConnectionFactory` object. `ConnectionFactory` provides an asynchronous connection creation method that accepts `Endpoint` object.

The following snippet connects to an ActiveMQ Artemis node on `localhost` on port `5672` as a `guest` user using `guest` as a password via insecure channel (AMQP). 

``` 
var connectionFactory = new ConnectionFactory();
var endpoint = Endpoint.Create("localhost", 5672, "guest", "guest");
var connection = await connectionFactory.CreateAsync(endpoint);
```

### Disconnecting from ActiveMQ Artemis

Closing connection, same as opening, is fully asynchronous operation.

To disconnect, simply call `DisposeAsync` on connection object:

``` 
await connection.DisposeAsync();
```

### Sending messages

ActiveMQ.Net uses `IProducer` and `IAnonymousProducer` interfaces for sending messages. In order to create instance of `IProducer` you need to specify an address name and a routing type to which messages will be sent. 

``` 
var producer = await connection.CreateProducerAsync("a1", AddressRoutingType.Anycast);
```

All messages send using this producer will be automatically routed to address `a1` using `Anycast` routing type:

``` 
await producer.SendAsync(new Message("foo"));
```

To send messages to other addresses you can create more producers or use `IAnonymousProducer` interface. `IAnonymousProducer` is not connected to any particular address.

```
var anonymousProducer = await connection.CreateAnonymousProducer();
```

Each time you want to send a message, you need to specify the address name and the routing type:

```
await anonymousProducer.SendAsync("a2", AddressRoutingType.Multicast, new Message("foo"));
```

### Receiving messages

ActiveMQ.Net uses `IConsumer` interface for receiving messages. `IConsumer` can be created as follows:

```
var consumer = await connection.CreateConsumerAsync("a1", QueueRoutingType.Anycast);
```

As soon as the subscription is set up, the messages will be delivered automatically as the arrive, and then buffered inside consumer object. The amount of buffered messages can be controlled by `Consumer Credit`. In order to actually get a message, simply call `ReceiveAsync` on `IConsumer`.

```
var message = await consumer.ReceiveAsync();
```

If there are no messages buffered `ReceiveAsync` will asynchronously wait, and returns as soon as the new message appears on the client.

This operation can potentially last an indefinite amount of time (if there are no messages), `ReceiveAsync` accepts `CancellationToken` that can be used to cancel the operation.

```
var cts = new CancellationTokenSource();
var message = await consumer.ReceiveAsync(cts.Token);
```

This may be particularly useful when you want to quickly shut down you application.

### Resources lifespan

Connections, producers and consumers are meant to be long-lived objects. The underlying protocol is designed and optimized for long running connections. That means that opening a new connection per operation, e.g. sending a message, is unnecessary and strongly discouraged as it will introduce a lot of network round trips and overhead. The same rule applies to all ActiveMQ.Net resources.