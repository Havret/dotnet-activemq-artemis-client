---
id: message-durability
title: Message Durability Modes
sidebar_label: Message Durability
---

ActiveMQ Artemis supports two types of durability modes for messages: `durable` and `nondurable`. By default each message sent by the client using `SendAsync` method is durable. That means that the broker actually has to persist the message on the disk before the confirmation frame (ack) will be sent back to the client. The confirmation frame is what we can *await* for.

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
    DurabilityMode = DurabilityMode.Nondurable // takes precedence over durability
                                               // mode specified on the producer level
});
```