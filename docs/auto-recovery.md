---
id: auto-recovery
title: Automatic Recovery From Network Failures
sidebar_label: Automatic Recovery
---

A network connection between clients and ActiveMQ Artemis nodes can fail. The client library supports the automatic recovery of connections, producers, and consumers. Automatic recovery is enabled by default.

Automatic recovery is controlled by `IRecoveryPolicy` interface:

```csharp
public interface IRecoveryPolicy
{
    int RetryCount { get; }
    TimeSpan GetDelay(int attempt);
}
```

This interface defines how long the delay should last between subsequent recovery attempts if recovery fails due to an exception (e.g. ActiveMQ Artemis node is still not reachable), and how many recovery attempts will be made before terminal exception will be signaled.

You can subscribe to this occurrence using `IConnection.ConnectionRecoveryError` event:

```csharp
var connectionFactory = new ConnectionFactory();
var connection = await connectionFactory.CreateAsync(endpoint);
connection.ConnectionRecoveryError += (sender, eventArgs) =>
{
    // react accordingly
};
```

When the client library successfully reestablishes the connection, `IConnection.ConnectionRecovered` event is triggered instead.

To retry indefinite amount of times `RetryCount` should return `int.MaxValue`.

:::tip

If the initial connection to an ActiveMQ Artemis node fails, automatic connection recovery will kick in as well. It may be problematic in some scenarios, as `ConnectionFactory.CreateAsync` won't signal any issues until the recovery policy gives up. If your recovery policy is configured to try to recover forever it may even never happen. That means you would be asynchronously waiting for the result of `CreateAsync` forever. To address this issue you can pass `CancellationToken` to `CreateAsync`. This allows you to arbitrarily break the operation at any point.

:::

## Recovery Policies

There are 3 built-in recovery policies that are available via `RecoveryPolicyFactory` class:

### Constant Backoff Recovery Policy

This policy instructs the connection recovery mechanism to wait a constant amount of time between recovery attempts.

The following defines a policy that will retry 5 times and wait 1s between each recovery attempt.

```csharp
var constantBackoff = RecoveryPolicyFactory.ConstantBackoff(
    delay: TimeSpan.FromSeconds(1),
    retryCount: 5);
```

### Linear Backoff Recovery Policy

This policy instructs the connection recovery mechanism to wait increasingly longer times between recovery attempts.

The following defines a policy with a linear retry delay of 100, 200, 300, 400, 500ms.

```csharp
var linearBackoff = RecoveryPolicyFactory.LinearBackoff(
    initialDelay: TimeSpan.FromMilliseconds(100),
    retryCount: 5);
```  

The default linear factor is 1.0. However, it can be changed:

```csharp
var linearBackoff = RecoveryPolicyFactory.LinearBackoff(
    initialDelay: TimeSpan.FromMilliseconds(100),
    retryCount: 5,
    factor: 2);
```

This will create an increasing retry delay of 100, 300, 500, 700, 900ms.

Note, the linear factor must be greater than or equal to zero. A factor of zero will return equivalent retry delays to the `ConstantBackoffRecoveryPolicy`.

When the infinite number of retires is used, it may be useful to specify a maximum delay:

```csharp
var linearBackoff = RecoveryPolicyFactory.LinearBackoff(
    initialDelay: TimeSpan.FromMilliseconds(100),
    maxDelay: TimeSpan.FromSeconds(15));
```

### Exponential Backoff Recovery Policy

This policy instructs the connection recovery mechanism to use the exponential function to calculate subsequent delays between recovery attempts. The delay duration is specified as `initialDelay x 2^attempt`. Because of the exponential nature (potential for rapidly increasing delay times), it is recommended to use this policy with a low starting delay, and explicitly setting maximum delay.

```csharp
var exponentialBackoff = RecoveryPolicyFactory.ExponentialBackoff(
    initialDelay: TimeSpan.FromMilliseconds(100),
    retryCount: 5);
```

This will create an exponentially increasing retry delay of 100, 200, 400, 800, 1600ms.

The default exponential growth factor is 2.0. However, you can provide our own.

```csharp
var exponentialBackoff = RecoveryPolicyFactory.ExponentialBackoff(
    initialDelay: TimeSpan.FromMilliseconds(100),
    retryCount: 5,
    factor: 4.0);
```

The upper for this retry with a growth factor of four is 25,600ms.

Note, the growth factor must be greater than or equal to one. A factor of one will return equivalent retry delays to the `ConstantBackoffRecoveryPolicy`.

## Recover from first failure fast

All build-in recovery policies include an option to recover after the first failure immediately. You can enable this by passing in `fastFirst: true` to any of the policy factory methods.

```csharp
var recoveryPolicy = RecoveryPolicyFactory.ExponentialBackoff(
    initialDelay: TimeSpan.FromMilliseconds(100),
    retryCount: 5,
    factor: 4.0,
    fastFirst: true);
```

Note, the first recovery attempt will happen immediately and it will count against your retry count. That is, this will still try to recover five times but the first recovery attempt will happen immediately after connection to the broker is lost.

The logic behind a fast first recovery strategy is that failure may just have been a transient blip rather than reflecting a deeper underlying issue that for instance results in a broker failover.

## Disabling Automatic Recovery

To disable automatic recovery, set `ConnectionFactory.AutomaticRecoveryEnabled` to false:

```csharp
var connectionFactory = new ConnectionFactory();
connectionFactory.AutomaticRecoveryEnabled = false;
// connection that will not recover automatically

var connection = await connectionFactory.CreateAsync(endpoint);
```

## Failover

To provide high availability your typical ActiveMQ Artemis cluster configuration should contain at least 2 nodes: a master and a slave. For most of the time, only the master node is operational and it handles all of the requests. When the master goes down, however, failover occurs and the slave node becomes active.

To handle this scenario with the client library you need to use `ConnectionFactory.CreateAsync` overload that accepts `IEnumerable<Endpoint>`. This way when the connection to the first node is lost, the auto-recovery mechanism will try to reconnect to the second node. The endpoints are selected in a round-robin fashion using the original sequence with which the connection was created.

```csharp
var masterEndpoint = Endpoint.Create(
    host: "master",
    port: 5672,
    user: "guest",
    password: "guest",
    scheme: Scheme.Amqp);
var slaveEndpoint = Endpoint.Create(
    host: "slave",
    port: 5672,
    user: "guest",
    password: "guest",
    scheme: Scheme.Amqp);
var connectionFactory = new ConnectionFactory();
var connection = await connectionFactory.CreateAsync(new[]
{
    masterEndpoint,
    slaveEndpoint
});
```