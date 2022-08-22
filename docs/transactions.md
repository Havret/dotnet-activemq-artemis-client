---
id: transactions
title: Transactions
sidebar_label: Transactions
---

ActiveMQ Artemis provides several guarantees regarding reliable message delivery and processing. In [Message Durability Modes](message-durability.md) section, you may learn how to send a single message so it won't be lost in transit.

If you need stronger guarantees that span over multiple messages you need to use transactions.

Let's image that as part of your processing flow you want to send a series of messages in a all or nothing manner:

```csharp
var connectionFactory = new ConnectionFactory();
var endpoint = Endpoint.Create("localhost", 5672, "guest", "guest");
var connection = await connectionFactory.CreateAsync(endpoint);

await using (var transaction = new Transaction())
{
    await producer.SendAsync(
        address: "credit-queue",
        message: new Message("credit:500,account:a"),
        transaction: transaction
    );
    await producer.SendAsync(
        address: "debit-queue",
        message: new Message("debit:500,account:b"),
        transaction: transaction);

    await transaction.CommitAsync();
}
```

Consumers attached to `credit-queue` and `debit-queue` won't see any messages until you call `CommitAsync` on the `Transaction` object. If any of these operations fail, it will be as if nothing happened. 

Message acknowledge operations can also participate in a transaction. The typical flow is that you receive a message [1], do some transformation, and publish another message [2] as a result. In order to be sure that the [1] will be acknowledged only if you successfully sent the [2], you can do sth like this:

```csharp
var endpoint = Endpoint.Create("localhost", 5672, "guest", "guest");
var connectionFactory = new ConnectionFactory();
await using var connection = await connectionFactory.CreateAsync(endpoint);
await using var producer = await connection.CreateProducerAsync("my-address-2", RoutingType.Anycast);
await using var consumer = await connection.CreateConsumerAsync("my-address-1", RoutingType.Anycast);

var msg1 = await consumer.ReceiveAsync();

await using (var transaction = new Transaction())
{
    // transactionally acknowledge message
    await consumer.AcceptAsync(msg1, transaction);

    var result = DoSomeProcessing(msg1);
    var msg2 = new Message(result);

    // transactionally send another message
    await producer.SendAsync(msg2, transaction);

    // commit the two operations
    await transaction.CommitAsync();
}
```