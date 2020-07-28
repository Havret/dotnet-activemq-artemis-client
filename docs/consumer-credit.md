---
id: consumer-credit
title: Consumer credit
sidebar_label: Consumer credit
---

A consumer can control the flow of messages from a broker by allocating *credit* for a particular number of messages. As the broker delivers messages, it spends this credit. Each delivered message results in decrementing the available message credit by one. Each message acknowledged by the client increments the available message credit by one.

## Configuring message credit on a consumer

**ActiveMQ Artemis Client** allows you to specify message credit via consumer configuration. By default, each new consumer is created with a message credit set to 200. This can be easily changed as follows:

```csharp
var consumer = await connection.CreateConsumerAsync(new ConsumerConfiguration
{
    Address = address,
    RoutingType = RoutingType.Multicast,
    Credit = 2 // Set message credit to 2
});
```

Credit value has to be greater or equal to 1. Otherwise `CreateConsumerAsync` will throw `ArgumentOutOfRangeException`.

:::info

Message credit cannot be changed after the consumer has been created.

:::

The above code snippet creates a consumer that can receive a maximum of 2 messages. If you don't acknowledge any of them, no further messages will be delivered by the broker.

:::caution

It is important to handle message acknowledgment properly in your application code, as *lost* messages can inexorably lead to running out of message credit and eventually block your consumers.

:::