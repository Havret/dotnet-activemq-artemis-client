---
name: unit-test-style
description: Guidelines and conventions for writing unit tests in this repo. Use when creating, reviewing, or discussing unit tests that run without a real broker.
---

# Unit Test Style Guide

This skill describes the required style for all unit tests in this repository. Follow it exactly when writing new tests or reviewing existing ones.

---

## Two Kinds of Unit Tests

This repo has two flavours of unit test, each with its own infrastructure:

| Kind | Projects | Infrastructure |
|---|---|---|
| **Core unit tests** | `test/ArtemisNetClient.UnitTests/` | `ActiveMQNetSpec` base class + `TestContainerHost` (in-process AMQP server) |
| **TestKit unit tests** | `test/ArtemisNetClient.Testing.UnitTests/` | `TestKit` (the in-process broker from the test kit library), no base class |
| **Pure model tests** | either project | No infrastructure — pure C# assertions |

Choose the infrastructure that matches what you're testing:
- Testing AMQP protocol behaviour, connection/producer/consumer lifecycle → **Core unit test**
- Testing `TestKit` itself → **TestKit unit test**
- Testing data types, factory methods, or pure logic → **Pure model test**

---

## File & Class Naming

- Files use the `*Spec.cs` suffix (BDD-style), e.g. `ConsumerReceiveMessageSpec.cs`
- Subtopics go in subdirectories, e.g. `AutoRecovering/AutoRecoveringProducerSpec.cs`

---

## Core Unit Tests — Base Class

All core unit test classes inherit from `ActiveMQNetSpec`:

```csharp
public class MyFeatureSpec : ActiveMQNetSpec
{
    public MyFeatureSpec(ITestOutputHelper output) : base(output) { }
}
```

The base class provides:

| Member | What it gives you |
|---|---|
| `GetUniqueEndpoint()` | Unique local endpoint via `EndpointUtil` |
| `CreateConnection(endpoint)` | `IConnection` via `ConnectionFactory` with logging and GUID message IDs |
| `CreateConnectionWithoutAutomaticRecovery(endpoint)` | Same but with auto-recovery disabled |
| `CreateConnectionFactory()` | Configured `ConnectionFactory` for customisation |
| `CreateTestLoggerFactory()` | `XUnitLoggerFactory` wired to `ITestOutputHelper` |
| `CreateOpenedContainerHost(endpoint?, handler?)` | Running in-process AMQP server |
| `CreateContainerHost(endpoint?, handler?)` | Same but not yet opened |
| `CreateContainerHostThatWillNeverSendAttachFrameBack(endpoint)` | Host that ignores link attachment |
| `DisposeHostAndWaitUntilConnectionNotified(host, conn)` | Drops the host and waits for the connection close event |
| `WaitUntilConnectionRecovered(conn)` | Waits for the connection recovered event |
| `Timeout` | 1 minute (DEBUG) / 10 seconds (Release) |
| `ShortTimeout` | 100 ms — use for negative assertions |
| `CancellationToken` | Token backed by `Timeout` |

---

## TestKit Unit Tests — No Base Class

TestKit specs do not inherit from `ActiveMQNetSpec`. Set up AMQP tracing in the constructor and use `EndpointUtil` directly:

```csharp
public class MyTestKitSpec
{
    public MyTestKitSpec(ITestOutputHelper output)
    {
        Trace.TraceLevel = TraceLevel.Information;
        var logger = new XUnitLogger(output, "logger");
        Trace.TraceListener += (_, format, args) => logger.LogTrace(format, args);
    }

    [Fact]
    public async Task Should_do_something()
    {
        var endpoint = EndpointUtil.GetUniqueEndpoint();
        using var testKit = new TestKit(endpoint);

        var connectionFactory = new ConnectionFactory();
        await using var connection = await connectionFactory.CreateAsync(endpoint);
        // ...
    }
}
```

---

## Pure Model Tests — No Infrastructure

For tests that only exercise data types, factory methods, or pure logic, omit both the base class and `ITestOutputHelper`:

```csharp
public class EndpointSpec
{
    [Fact]
    public void Should_create_endpoint()
    {
        var endpoint = Endpoint.Create("localhost", 5672, "guest", "guest");
        Assert.Equal("localhost", endpoint.Host);
    }
}
```

Synchronous `[Fact]` tests are fine here — only use `async Task` when you're actually doing async I/O.

---

## Test Method Conventions

- Core and TestKit tests are `async Task` — no synchronous tests unless there is no async I/O at all
- Method names read as sentences: `Should_receive_message`, `Throws_when_invalid_scheme_specified`
- Both `[Fact]` and `[Theory]` are allowed; use `[Theory]` with `[MemberData]` or `[InlineData]` when the same behaviour must be verified across a set of inputs

```csharp
[Fact]
public async Task Should_do_something_meaningful()
{
    // ...
}

[Theory, MemberData(nameof(RoutingTypesData))]
public async Task Should_behave_differently_per_routing_type(RoutingType routingType, object expected)
{
    // ...
}
```

---

## In-Process AMQP Infrastructure

Use the helpers from `ActiveMQNetSpec` to build a local AMQP server:

```csharp
// Simple server — no custom handler
using var host = CreateOpenedContainerHost(endpoint);

// Intercept AMQP frames
var testHandler = new TestHandler(@event =>
{
    switch (@event.Id)
    {
        case EventId.ConnectionRemoteOpen:
            // ...
            break;
        case EventId.LinkRemoteOpen when @event.Context is Attach attach && !attach.Role:
            // ...
            break;
    }
});
using var host = CreateOpenedContainerHost(endpoint, testHandler);

// Enqueue messages for a consumer to pick up
var messageSource = host.CreateMessageSource("my-address");
messageSource.Enqueue(new Message("foo"));

// Intercept messages sent by a producer — synchronous dequeue
var messageProcessor = host.CreateMessageProcessor("my-address");
// ...send a message...
var received = messageProcessor.Dequeue(Timeout);

// Low-level link control
var linkProcessor = host.CreateTestLinkProcessor();
linkProcessor.SetHandler(_ => true); // suppress Attach frames
```

`TestContainerHost` implements `IDisposable` — always declare it with `using var`:

```csharp
using var host = CreateOpenedContainerHost(endpoint);
```

---

## Resource Management

- `TestContainerHost` → `using var` (synchronous `IDisposable`)
- `IConnection` → `await using var` (asynchronous `IAsyncDisposable`)
- `IProducer` / `IConsumer` → `await using var` when you want automatic cleanup; declare without `await using` when you need to `DisposeAsync()` mid-test

When a test owns several resources whose lifetimes end together, use `DisposeUtil.DisposeAll`:

```csharp
await DisposeUtil.DisposeAll(producer, connection, host);
```

---

## Synchronisation

### Async event waits — use `TaskCompletionSource<T>`

```csharp
var tcs = new TaskCompletionSource<bool>();
using var cts = new CancellationTokenSource(Timeout);
await using var _ = cts.Token.Register(() => tcs.TrySetCanceled());
connection.ConnectionClosed += (_, _) => tcs.TrySetResult(true);
// trigger the event...
await tcs.Task;
```

### Sync waits inside `TestHandler` — use `ManualResetEvent` or `CountdownEvent`

```csharp
var attached = new ManualResetEvent(false);
var handler = new TestHandler(@event =>
{
    if (@event.Id == EventId.LinkRemoteOpen)
        attached.Set();
});
// ...
Assert.True(attached.WaitOne(Timeout));
```

### Negative assertions (confirming something does NOT happen)

For async paths, cancel with `ShortTimeout`:

```csharp
var cts = new CancellationTokenSource(ShortTimeout);
await Assert.ThrowsAnyAsync<OperationCanceledException>(
    async () => await consumer.ReceiveAsync(cts.Token));
```

For synchronous paths via `ManualResetEvent`, assert `WaitOne` returns false:

```csharp
Assert.False(producerAttached.WaitOne(ShortTimeout));
```

---

## Arrange-Act-Assert Structure

Follow a clear, unlabelled AAA structure. Keep each section visually separate with a blank line:

```csharp
[Fact]
public async Task Should_receive_message()
{
    var endpoint = GetUniqueEndpoint();
    using var host = CreateOpenedContainerHost(endpoint);
    var messageSource = host.CreateMessageSource("a1");
    await using var connection = await CreateConnection(endpoint);
    var consumer = await connection.CreateConsumerAsync("a1", RoutingType.Anycast);

    messageSource.Enqueue(new Message("foo"));
    var message = await consumer.ReceiveAsync();

    Assert.NotNull(message);
    Assert.Equal("foo", message.GetBody<string>());
}
```

The one exception is `Should_create_connection_with_specified_client_id`-style tests where the AAA sections are long and a `// Arrange` / `// Act` / `// Assert` comment genuinely helps navigation — add them only in that case.

---

## Assertions

Use plain xUnit assertions — no custom extensions, no FluentAssertions:

```csharp
Assert.Equal("expected", actual);
Assert.NotNull(value);
Assert.Null(value);
Assert.True(condition);
Assert.False(condition);
Assert.Same(expected, actual);
Assert.IsType<ConcreteType>(instance);
Assert.Contains("substring", message);
Assert.Throws<ArgumentNullException>(() => { ... });
await Assert.ThrowsAsync<ObjectDisposedException>(() => connection.CreateProducerAsync(...));
await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await consumer.ReceiveAsync(cts.Token));
Assert.True(manualResetEvent.WaitOne(Timeout));
```

---

## Private Helper Methods

Extract repeated sequences into private helpers within the same class:

```csharp
private async Task ShouldSendMessageWithPayload<T>(T payload)
{
    using var host = CreateOpenedContainerHost();
    var messageProcessor = host.CreateMessageProcessor("a1");
    await using var connection = await CreateConnection(host.Endpoint);
    await using var producer = await connection.CreateProducerAsync("a1", RoutingType.Anycast);

    await producer.SendAsync(new Message(payload));

    var received = messageProcessor.Dequeue(Timeout);
    Assert.Equal(payload, received.GetBody<T>());
}

private async Task<(IProducer, MessageProcessor, TestContainerHost, IConnection)> CreateReattachedProducer()
{
    // ...
}
```

Local static functions are also fine for single-test helpers (C# 9+):

```csharp
static async Task SendMessagesToGroup(TestKit testKit, string address, string groupId, int count) { ... }
```

---

## Comments

Add a comment only when the WHY is non-obvious — a hidden constraint, a workaround, or behaviour that would surprise a reader:

```csharp
// do not send outcome from a remote peer — as a result send should timeout
messageProcessor.SetHandler(_ => true);

// run on another thread as we don't want to block here
var produceTask = Task.Run(() => producer.Send(new Message("foo")));
```

Never describe what the code does — well-named identifiers already do that.

---

## Summary Checklist

When writing a new unit test, verify:

- [ ] File is named `*Spec.cs` and is in the correct project / subdirectory
- [ ] Test class uses the right infrastructure (inherits `ActiveMQNetSpec`, uses `TestKit`, or has no base class)
- [ ] Constructor injects `ITestOutputHelper` when the class inherits `ActiveMQNetSpec`
- [ ] Core and TestKit test methods are `async Task`; pure model methods may be synchronous
- [ ] `TestContainerHost` uses `using var`; connections/producers/consumers use `await using var`
- [ ] Event waits use `TaskCompletionSource` (async) or `ManualResetEvent` / `CountdownEvent` (sync inside `TestHandler`)
- [ ] Negative assertions use `ShortTimeout` (100 ms), not `Timeout`
- [ ] Assertions use plain xUnit — no third-party assertion libraries
- [ ] `[Theory]` used only when the same behaviour needs to be verified across multiple input sets
