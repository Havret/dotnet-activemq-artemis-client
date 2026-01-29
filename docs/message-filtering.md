---
id: message-filtering
title: Message Filtering
sidebar_label: Message Filtering
---

Message filtering allows consumers to selectively receive messages based on specific criteria. **ActiveMQ Artemis Client** supports two types of filtering: filter expressions and the NoLocal filter.

## Filter Expressions

Filter expressions enable consumers to receive only messages that match specific criteria based on message properties or headers. You can specify a filter expression using SQL-like syntax:

```csharp
var consumer = await connection.CreateConsumerAsync(new ConsumerConfiguration
{
    Address = address,
    RoutingType = RoutingType.Multicast,
    FilterExpression = "color = 'red'"
});
```

With this configuration, the consumer will only receive messages where the `color` property equals `red`.

## NoLocal Filter

The NoLocal filter prevents a consumer from receiving messages that were sent from the same connection. This is particularly useful in publish-subscribe scenarios where you want to receive messages from other clients but not from your own connection.

### Configuration

To enable the NoLocal filter, set the `NoLocalFilter` property to `true` in the consumer configuration:

```csharp
var consumer = await connection.CreateConsumerAsync(new ConsumerConfiguration
{
    Address = address,
    RoutingType = RoutingType.Multicast,
    NoLocalFilter = true
});
```

### Example

The following example demonstrates how the NoLocal filter works in practice:

```csharp
var address = "news.updates";

// Create first connection with producer and consumer
await using var connection1 = await connectionFactory.CreateAsync(endpoint);
await using var producer1 = await connection1.CreateProducerAsync(address, RoutingType.Multicast);
await using var consumer = await connection1.CreateConsumerAsync(new ConsumerConfiguration
{
    Address = address,
    RoutingType = RoutingType.Multicast,
    NoLocalFilter = true
});

// Create second connection with producer
await using var connection2 = await connectionFactory.CreateAsync(endpoint);
await using var producer2 = await connection2.CreateProducerAsync(address, RoutingType.Multicast);

// Send messages from both connections
await producer1.SendAsync(new Message("from connection 1"));
await producer2.SendAsync(new Message("from connection 2"));

// Consumer will only receive the message from connection 2
var message = await consumer.ReceiveAsync(cancellationToken);
Console.WriteLine(message.GetBody<string>()); // Output: "from connection 2"
```

In this example, even though both producers send messages to the same address, the consumer only receives the message from `connection2` because the message from `connection1` is filtered out by the NoLocal filter.

:::caution

**Important:** The NoLocal filter only works with the **Multicast** routing type. Attempting to use it with Anycast routing type will result in an `ArgumentException` when creating the consumer.

:::

## Combining Filters

You can use both filter expressions and the NoLocal filter together:

```csharp
var consumer = await connection.CreateConsumerAsync(new ConsumerConfiguration
{
    Address = address,
    RoutingType = RoutingType.Multicast,
    FilterExpression = "priority > 5",
    NoLocalFilter = true
});
```

This consumer will only receive messages that:

1. Have a `priority` property greater than 5, AND
2. Were not sent from the same connection
