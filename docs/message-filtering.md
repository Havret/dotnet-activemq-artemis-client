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

### Syntax and Operators

Filter expressions use a SQL-92 syntax similar to JMS message selectors. The following operators and constructs are supported:

#### Comparison Operators

- `=` - Equal to
- `<>` or `!=` - Not equal to
- `>` - Greater than
- `>=` - Greater than or equal to
- `<` - Less than
- `<=` - Less than or equal to

#### Logical Operators

- `AND` - Logical AND
- `OR` - Logical OR
- `NOT` - Logical NOT

#### Other Operators

- `BETWEEN` - Range comparison (e.g., `priority BETWEEN 5 AND 9`)
- `IN` - Set membership (e.g., `color IN ('red', 'blue', 'green')`)
- `LIKE` - String pattern matching with wildcards (`%` for multiple characters, `_` for single character)
- `IS NULL` / `IS NOT NULL` - Check for null values

### Predefined AMQP Message Properties

ActiveMQ Artemis provides access to standard AMQP message properties through predefined identifiers:

#### AMQPriority

Filters messages based on their priority level (0-9, where 9 is highest priority):

```csharp
var consumer = await connection.CreateConsumerAsync(new ConsumerConfiguration
{
    Address = address,
    RoutingType = RoutingType.Anycast,
    FilterExpression = "AMQPriority = 9"
});
```

You can also use comparison operators:

```csharp
FilterExpression = "AMQPriority > 5"  // Only high-priority messages
```

#### AMQExpiration

Filters messages based on their expiration time (absolute timestamp in milliseconds):

```csharp
var messageExpiryTime = DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeMilliseconds();

var consumer = await connection.CreateConsumerAsync(new ConsumerConfiguration
{
    Address = address,
    RoutingType = RoutingType.Anycast,
    FilterExpression = $"AMQExpiration > {messageExpiryTime}"
});
```

This filters for messages that expire after the specified time.

#### AMQDurable

Filters messages based on their durability mode:

```csharp
var consumer = await connection.CreateConsumerAsync(new ConsumerConfiguration
{
    Address = address,
    RoutingType = RoutingType.Anycast,
    FilterExpression = "AMQDurable = 'DURABLE'"
});
```

Valid values are:
- `'DURABLE'` - For durable messages
- `'NON_DURABLE'` - For non-durable messages

#### AMQTimestamp

Filters messages based on their creation timestamp (in milliseconds since Unix epoch):

```csharp
var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

var consumer = await connection.CreateConsumerAsync(new ConsumerConfiguration
{
    Address = address,
    RoutingType = RoutingType.Anycast,
    FilterExpression = $"AMQTimestamp > {timestamp}"
});
```

This example filters for messages created after the current time.

### Custom Application Properties

You can filter messages based on custom application properties that you set when sending messages:

```csharp
// Sending a message with custom properties
await producer.SendAsync(new Message("order data")
{
    ApplicationProperties =
    {
        ["color"] = "red",
        ["size"] = "large",
        ["quantity"] = 10
    }
});

// Filtering by custom property
var consumer = await connection.CreateConsumerAsync(new ConsumerConfiguration
{
    Address = address,
    RoutingType = RoutingType.Anycast,
    FilterExpression = "color = 'red'"
});
```

Application properties support various data types including strings, numbers, and booleans.

### Complex Filter Expressions

You can combine multiple conditions using logical operators:

#### Using AND

Receive only messages that match all conditions:

```csharp
FilterExpression = "color = 'red' AND size = 'large'"
```

#### Using OR

Receive messages that match any of the conditions:

```csharp
FilterExpression = "color = 'red' OR color = 'blue'"
```

#### Combining Multiple Operators

Create sophisticated filters by combining various operators:

```csharp
FilterExpression = "AMQPriority > 5 AND (color = 'red' OR color = 'blue')"
```

```csharp
FilterExpression = "quantity BETWEEN 10 AND 100 AND status = 'pending'"
```

#### Using IN Operator

Filter for multiple possible values:

```csharp
FilterExpression = "color IN ('red', 'blue', 'green')"
```

#### Pattern Matching with LIKE

Use wildcards for string matching:

```csharp
FilterExpression = "department LIKE 'sales%'"  // Starts with 'sales'
FilterExpression = "email LIKE '%@example.com'"  // Ends with '@example.com'
FilterExpression = "code LIKE 'A_B'"  // 'A', any character, then 'B'
```

### Handling NULL Values

You can filter for messages where a property is missing or explicitly null:

```csharp
var consumer = await connection.CreateConsumerAsync(new ConsumerConfiguration
{
    Address = address,
    RoutingType = RoutingType.Multicast,
    FilterExpression = "optionalField IS NULL"
});
```

Or check that a property exists:

```csharp
FilterExpression = "requiredField IS NOT NULL"
```

### Example: Multi-Criteria Filtering

Here's a complete example demonstrating complex filtering:

```csharp
await using var connection = await connectionFactory.CreateAsync(endpoint);
var address = "orders";

// Create consumer that only receives high-priority, pending orders for the premium tier
await using var consumer = await connection.CreateConsumerAsync(new ConsumerConfiguration
{
    Address = address,
    RoutingType = RoutingType.Anycast,
    FilterExpression = "AMQPriority >= 7 AND status = 'pending' AND tier = 'premium'"
});

// Create producer and send various messages
await using var producer = await connection.CreateProducerAsync(address, RoutingType.Anycast);

// This message will be received (matches all criteria)
await producer.SendAsync(new Message("order1")
{
    Priority = 9,
    ApplicationProperties =
    {
        ["status"] = "pending",
        ["tier"] = "premium"
    }
});

// This message will NOT be received (priority too low)
await producer.SendAsync(new Message("order2")
{
    Priority = 4,
    ApplicationProperties =
    {
        ["status"] = "pending",
        ["tier"] = "premium"
    }
});

// This message will NOT be received (wrong status)
await producer.SendAsync(new Message("order3")
{
    Priority = 8,
    ApplicationProperties =
    {
        ["status"] = "completed",
        ["tier"] = "premium"
    }
});

var message = await consumer.ReceiveAsync(cancellationToken);
Console.WriteLine(message.GetBody<string>()); // Output: "order1"
```

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
