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