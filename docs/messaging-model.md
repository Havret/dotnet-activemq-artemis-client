---
id: messaging-model
title:  Messaging Model
sidebar_label: Messaging Model
---

Apache ActiveMQ Artemis messaging model is build on top of three main concepts: *addresses*, *queues* and *routing types*.

# Address
The general idea behind the messaging model of Apache ActiveMQ Artemis is that producers never send messages directly to queues. Actually, a producer is unaware whether a message will be delivered to any queue at all. Instead, the producer can only send messages to an address. The address represents a message endpoint. It receives messages from producers and pushes them to queues. The address knows exactly what to do with the message it receives. Should it be appended to a single or to many queues? Or maybe should it be discarded. The rules for that are defined by the *routing type*.

# Routing Types
There are two routing types available in Apache ActiveMQ Artemis: *Anycast* and *Multicast*. The address can be created with either or both routing types.

When the address was created with *Anycast* routing type all messages send to this address will be evenly distributed[^anycast-message-distribution] among all the queues attached to this address. With *Multicast* routing type every queue bound to the address receives its very own copy of a message. Had the message been pushed to the queue, it can be picked up by one of the consumers attached to this particular queue.

# Further Reading
The Apache ActiveMQ Artemis address model is described in detail (with examples using ArtemisNetClient) in [this](https://havret.io/activemq-artemis-address-model) article.

### Footnotes:
[^anycast-message-distribution]: Messages will be distributed using Round-robin distribution algorithm.