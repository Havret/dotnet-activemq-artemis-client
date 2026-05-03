---
id: message-acknowledgment
title: Message Acknowledgment
sidebar_label: Message Acknowledgment
---

After receiving a message, the consumer must signal the broker with the outcome of message processing. The client supports four acknowledgment modes that map directly to AMQP 1.0 delivery outcomes: **Accept**, **Reject**, **Modify**, and implicit **Release** (on consumer close without acknowledgment).

## Accept

`AcceptAsync` tells the broker that the message was successfully processed. The broker removes the message from the queue.

```csharp
var message = await consumer.ReceiveAsync();
// process message...
await consumer.AcceptAsync(message);
```

`AcceptAsync` can also participate in a transaction. See [Transactions](transactions.md) for details.

## Reject

`Reject` tells the broker that the message cannot be processed. The broker moves the message to the Dead Letter Queue (DLQ).

```csharp
var message = await consumer.ReceiveAsync();
consumer.Reject(message);
```

:::important

Artemis must have a dead letter address configured for the queue, otherwise the rejected message will be dropped. By default, Artemis routes rejected messages to the `DLQ` address.

:::

## Modify

`Modify` releases the message back to the queue with additional hints for the broker. It accepts two flags:

- `deliveryFailed` — when `true`, the broker increments the message's delivery count. This is useful when you want to track how many times a message has been retried.
- `undeliverableHere` — when `true`, the broker will not redeliver the message to this consumer. If another consumer is available on the same queue, the message will be dispatched there instead.

### Retry on the same consumer

To return the message to the queue and allow the same consumer to receive it again:

```csharp
var message = await consumer.ReceiveAsync();
consumer.Modify(message, deliveryFailed: true, undeliverableHere: false);
```

The message is put back and the delivery count is incremented by one.

### Redirect to a different consumer

To signal that this consumer cannot handle the message, but another consumer on the same queue might:

```csharp
var message = await consumer.ReceiveAsync();
consumer.Modify(message, deliveryFailed: true, undeliverableHere: true);
```

The message is requeued, its delivery count is incremented, and the broker will not send it back to this consumer.

### Requeue without incrementing delivery count

Setting `deliveryFailed: false` requeues the message without incrementing its delivery count:

```csharp
var message = await consumer.ReceiveAsync();
consumer.Modify(message, deliveryFailed: false, undeliverableHere: true);
```

This is useful when you want to hand off the message to another consumer without recording a failed delivery attempt.

## Release (implicit)

If a consumer is disposed without acknowledging a message, the broker automatically releases the message back to the queue. The delivery count is not incremented. The message becomes available for redelivery to any consumer.

```csharp
{
    await using var consumer = await connection.CreateConsumerAsync("a1", RoutingType.Anycast);
    var message = await consumer.ReceiveAsync();
    // consumer disposed here without AcceptAsync/Reject/Modify
    // message is released back to the queue
}
```

## Delivery count

The `DeliveryCount` property on a received message reflects how many times it has been redelivered. The initial delivery has a count of `0`. Each call to `Modify` with `deliveryFailed: true` increments this by one.

```csharp
var message = await consumer.ReceiveAsync();
if (message.DeliveryCount >= 3)
{
    // too many retries, reject to DLQ
    consumer.Reject(message);
}
else
{
    // retry later
    consumer.Modify(message, deliveryFailed: true, undeliverableHere: false);
}
```
