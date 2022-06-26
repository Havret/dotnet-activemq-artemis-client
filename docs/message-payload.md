---
id: message-payload
title: Message Payload
sidebar_label: Message Payload
---

The client uses `Message` class to represent messages which may be transmitted. A `Message` can carry various types of payload and accompanying metadata.

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

An attempt to pass an argument out of this list will result in `ArgumentOutOfRangeException`. Passing `null` is not acceptable either and will cause `ArgumentNullException`.

In order to get the message payload call `GetBody` and specify the expected type of the body section:

```csharp
var body = message.GetBody<T>();
```

If `T` matches the type of the payload, the value will be returned, otherwise, you will get `default(T)`.