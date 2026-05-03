---
name: integration-test-style
description: Guidelines and conventions for writing integration tests in this repo. Use when creating, reviewing, or discussing integration tests against the real Artemis broker.
---

# Integration Test Style Guide

This skill describes the required style for all integration tests in this repository. Follow it exactly when writing new tests or reviewing existing ones.

---

## File & Class Naming

- Files use the `*Spec.cs` suffix (BDD-style), e.g. `MessageAcknowledgementSpec.cs`
- Core tests live in `test/ArtemisNetClient.IntegrationTests/`
- Extension tests live in their own `*.IntegrationTests/` project
- Subtopics go in subdirectories (e.g. `TopologyManagement/CreateAddressSpec.cs`)

---

## Base Class

All core integration test classes inherit from `ActiveMQNetIntegrationSpec`:

```csharp
public class MyFeatureSpec : ActiveMQNetIntegrationSpec
{
    public MyFeatureSpec(ITestOutputHelper output) : base(output) { }
}
```

The base class provides:
- `CreateConnection()` — returns an `IConnection` backed by a `ConnectionFactory` wired to `XUnitLoggerFactory` with GUID message IDs
- `CancellationToken` — 1 minute in DEBUG, 10 seconds in Release
- `GetEndpoint()` — resolves broker address from env vars (`ARTEMIS_HOST`, `ARTEMIS_PORT`, `ARTEMIS_USERNAME`, `ARTEMIS_PASSWORD`; all with sensible defaults)

---

## Test Method Conventions

- Every test is `async Task` — no synchronous tests
- Use `[Fact]` for most tests; use `[Theory]` with `[InlineData]` or `[MemberData]` when the same behaviour must be verified across a set of inputs
- Method names read as sentences: `Should_acknowledge_message`, `Should_send_message_with_priority`

```csharp
[Fact]
public async Task Should_do_something_meaningful()
{
    // ...
}

[Theory, InlineData(RoutingType.Anycast), InlineData(RoutingType.Multicast)]
public async Task Should_behave_the_same_for_all_routing_types(RoutingType routingType)
{
    // ...
}
```

---

## Arrange-Act-Assert Structure

Tests follow a clear, unlabelled AAA structure. Keep each section visually separate with a blank line.

```csharp
[Fact]
public async Task Should_acknowledge_message()
{
    await using var connection = await CreateConnection();
    var address = Guid.NewGuid().ToString();
    await using var producer = await connection.CreateAnonymousProducerAsync(CancellationToken);
    await using var consumer = await connection.CreateConsumerAsync(address, RoutingType.Anycast, CancellationToken);

    await producer.SendAsync(address, RoutingType.Anycast, new Message("foo"), CancellationToken);
    var msg = await consumer.ReceiveAsync(CancellationToken);
    await consumer.AcceptAsync(msg);

    await consumer.DisposeAsync();
    var consumer2 = await connection.CreateConsumerAsync(address, RoutingType.Anycast, CancellationToken);
    await Assert.ThrowsAsync<OperationCanceledException>(
        async () => await consumer2.ReceiveAsync(
            new CancellationTokenSource(TimeSpan.FromMilliseconds(500)).Token));
}
```

---

## Resource Management

- Declare all disposables with `await using var` so they are cleaned up automatically even when a test fails
- When you need to dispose mid-test (to verify post-disposal state), call `await x.DisposeAsync()` explicitly at that point — do not declare with `await using` in that case

---

## Test Isolation

Use `Guid.NewGuid().ToString()` for every address, queue name, group ID, or any other broker resource name. Never hardcode strings that could collide across parallel test runs.

```csharp
var address = Guid.NewGuid().ToString();
var queue   = Guid.NewGuid().ToString();
```

---

## Cancellation & Negative Assertions

Use `CancellationToken` (from the base class) for all normal receive calls. For negative assertions (verifying that a message does *not* arrive), use a short inline token:

```csharp
await Assert.ThrowsAsync<OperationCanceledException>(
    async () => await consumer.ReceiveAsync(
        new CancellationTokenSource(TimeSpan.FromMilliseconds(500)).Token));
```

---

## Assertions

Use plain xUnit assertions — no custom extensions, no FluentAssertions:

```csharp
Assert.Equal("expected", actual);
Assert.NotNull(value);
Assert.Single(collection);
Assert.All(collection, item => Assert.Equal("x", item));
await Assert.ThrowsAsync<InvalidOperationException>(...);
await Assert.ThrowsAnyAsync<Exception>(...);
```

---

## Private Helper Methods

Extract repeated sequences into `private static async Task` helpers within the same class:

```csharp
private static async Task<IReadOnlyList<Message>> ReceiveMessages(
    IConsumer consumer, int count)
{
    var messages = new List<Message>();
    for (int i = 0; i < count; i++)
    {
        var msg = await consumer.ReceiveAsync(CancellationToken);
        await consumer.AcceptAsync(msg);
        messages.Add(msg);
    }
    return messages;
}
```

---

## DI / Hosting Extension Tests

Tests for `ArtemisNetClient.Extensions.DependencyInjection` or `Hosting` do **not** use `ActiveMQNetIntegrationSpec`. Instead they use the local `TestFixture`:

```csharp
public class ProducerSpec
{
    private readonly ITestOutputHelper _testOutputHelper;

    public ProducerSpec(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public async Task Should_register_producer()
    {
        var address = Guid.NewGuid().ToString();

        await using var fixture = await TestFixture.CreateAsync(
            _testOutputHelper,
            builder => builder.AddProducer<TestProducer>(address, RoutingType.Anycast));

        var producer = fixture.Services.GetRequiredService<TestProducer>();
        Assert.NotNull(producer);
    }
}
```

`TestFixture` wraps an `IHost`, exposes `Services`, `Connection`, and `CancellationToken`, and implements `IAsyncDisposable`.

---

## Multi-Step Scenario Tests

For complex flows with several distinct steps, use `NScenario` with `XUnitOutputAdapter` to produce readable output:

```csharp
var scenario = TestScenarioFactory.Default(new XUnitOutputAdapter(_testOutputHelper));

var fixture = await scenario.Step("Set up consumers", async () =>
    await TestFixture.CreateAsync(_testOutputHelper, builder =>
        builder.AddSharedDurableConsumer(address, queue, ...)));

await scenario.Step("Send messages", async () => { ... });
await scenario.Step("Verify distribution", async () => { ... });
```

Only use `NScenario` when a test has three or more meaningful named steps; simpler tests use plain AAA.

---

## Summary Checklist

When writing a new integration test, verify:

- [ ] Class is in the correct `*.IntegrationTests` project
- [ ] File is named `*Spec.cs`
- [ ] Class inherits `ActiveMQNetIntegrationSpec` (or uses `TestFixture` for DI tests)
- [ ] Constructor injects `ITestOutputHelper` and passes it to `base(output)`
- [ ] All test methods are `async Task` with `[Fact]` or `[Theory]`
- [ ] All broker resource names use `Guid.NewGuid().ToString()`
- [ ] All disposables use `await using var`
- [ ] `CancellationToken` from the base class is passed to every receive call
- [ ] Negative assertions use a short `CancellationTokenSource` timeout
- [ ] Assertions use plain xUnit — no third-party assertion libraries
