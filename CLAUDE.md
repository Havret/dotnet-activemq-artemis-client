# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

### Build
```bash
dotnet build ActiveMQ.Artemis.Client.sln
```

### Unit Tests (no broker required)
```bash
dotnet test --filter "FullyQualifiedName!~IntegrationTests"
```

### Integration Tests (requires broker)
Start the broker first:
```bash
cd test/artemis && docker-compose up -V -d
```

Then run:
```bash
dotnet test --filter "FullyQualifiedName~IntegrationTests"
```

Run a single test class:
```bash
dotnet test --filter "FullyQualifiedName~FilterExpressionsSpec"
```

### Integration Test Configuration
The broker endpoint is resolved from environment variables with these defaults:
- `ARTEMIS_HOST` → `localhost`
- `ARTEMIS_PORT` → `5672`
- `ARTEMIS_USERNAME` → `artemis`
- `ARTEMIS_PASSWORD` → `artemis`

## Architecture

### Core Library (`src/ArtemisNetClient`)

Built on top of [AmqpNetLite](http://azure.github.io/amqpnetlite/). The public API surface is:
- `ConnectionFactory` — entry point; configures recovery policy, message ID policy, TLS, TCP settings
- `IConnection` — creates producers, consumers, topology managers, request-reply clients
- `IProducer` / `IAnonymousProducer` — sends messages to a named address or any address
- `IConsumer` — receives messages with optional `FilterExpression`
- `IRequestReplyClient` — request-reply over AMQP
- `ITopologyManager` — creates/declares/removes addresses and queues

**Auto-recovery** (`AutoRecovering/`) wraps every public interface with a decorator that transparently reconnects and re-establishes links on broker disconnection. The recovery loop runs in a background `Task` per connection and coordinates via an unbounded `Channel<ConnectCommand>`.

**Message model** (`Message/`) maps cleanly to AMQP sections: `Properties`, `Header`, `ApplicationProperties`, `MessageAnnotations`. Filter expressions use JMS-selector/SQL-92 syntax and operate on `ApplicationProperties` and predefined AMQP identifiers (`AMQPriority`, `AMQExpiration`, `AMQDurable`, `AMQTimestamp`). String comparisons in filters are case-sensitive.

### Extensions

| Package | Purpose |
|---|---|
| `ArtemisNetClient.Extensions.DependencyInjection` | `IServiceCollection` integration; typed producers/consumers as hosted services |
| `ArtemisNetClient.Extensions.Hosting` | `IHostedService` wrappers |
| `ArtemisNetClient.Extensions.App.Metrics` | App.Metrics instrumentation |
| `ArtemisNetClient.Extensions.CloudEvents` | CloudEvents encoding/decoding on messages |
| `ArtemisNetClient.Extensions.LeaderElection` | Distributed leader election via durable queues |

### Test Kit (`src/ArtemisNetClient.Testing`)

`TestKit` spins up an in-process AMQP broker (via AmqpNetLite's `ContainerHost`) for unit testing messaging logic without a real broker. It supports filter expressions, shared message sources, and transactions. Use `TestKit` for fast unit tests; use the Docker broker for integration tests that verify real Artemis behavior.

### Test Projects

- `ArtemisNetClient.UnitTests` / `ArtemisNetClient.Testing.UnitTests` — pure unit tests, no broker
- `ArtemisNetClient.IntegrationTests` — tests against real Artemis; base class is `ActiveMQNetIntegrationSpec`
- `ArtemisNetClient.Extensions.IntegrationTests` / others — extension-specific integration tests
- `ArtemisNetClient.TestUtils` — shared helpers (`EndpointUtil`, test loggers)

### Documentation

The `docs/` directory is the source for the Docusaurus website in `website/`. Docs are published to GitHub Pages.
