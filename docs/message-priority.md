---
id: message-priority
title: Message Priority
sidebar_label: Message Priority
---

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