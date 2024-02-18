---
id: connection-lifecycle
title: Connection Lifecycle
sidebar_label: Connection Lifecycle
---

Once the connection to the broker has been established, the `IConnection` interface can notify you about several important events that may occur throughout the connection lifecycle.

## Connection Closed
`IConnection.ConnectionClosed` will be raised when the connection to the broker has been lost.

```csharp
var connectionFactory = new ConnectionFactory();
var connection = await connectionFactory.CreateAsync(endpoint);
connection.ConnectionClosed += (sender, eventArgs) =>
{
    // react accordingly
};
```

When the connection was created with [enabled automatic recovery](auto-recovery.md), this event may be raised multiple times. Without [automatic recovery](auto-recovery.md) this notification signifies that you won't be able to use this connection anymore.

`ConnectionClosed` event is raised with `ConnectionClosedEventArgs` parameter. You can inspect it to check the error that caused the connection to be terminated:

```csharp
connection.ConnectionClosed += (sender, eventArgs) =>
{
    // reason the connection was closed  
    string reason = eventArgs.Error;
    _logger.LogWarning(reason);
};
```

Or you can verify whether the connection was closed by the remote peer (broker) or not:

```csharp
connection.ConnectionClosed += (sender, eventArgs) =>
{
    bool closedByPeer = eventArgs.ClosedByPeer;
    if (closedByPeer) 
    {
        _logger.LogWarning("connection was closed by the broker");
    }
    else
    {
        _logger.LogWarning("connection was closed by the client");
    }
};
```

You can also check if the connection is open by inspecting `IsOpened` property:

```csharp
var connectionFactory = new ConnectionFactory();
var connection = await connectionFactory.CreateAsync(endpoint);
if (connection.IsOpened)
{
    // connection is still opened
}
else
{
    // connection to the broker has been lost
}

## Maintaining Connection Stability with TCP Keep-Alive

To enhance connection stability and prevent unexpected disconnections, consider configuring TCP Keep-Alive settings. This feature sends periodic signals to ensure the connection remains active, especially useful in unstable network environments.

```csharp
var factory = new ConnectionFactory();
// Configure TCP Keep-Alive settings
factory.TCP.KeepAliveTime = 30_000; // 30 seconds
factory.TCP.KeepAliveInterval = 1000; // 1 second
```

By setting `KeepAliveTime` and `KeepAliveInterval`, you can help ensure that your application maintains a reliable connection to the broker, complementing the event-driven connection management strategies outlined above.
