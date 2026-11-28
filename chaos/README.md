# Chaos suite

A [Jepsen](https://jepsen.io)-style harness that checks whether the client keeps its documented promises while the
network and the broker misbehave.

Concurrent clients run a workload against Artemis while a *nemesis* injects faults. Every operation is recorded in a
*history*. When the run ends, *checkers* compare the history against what the library promises, and they report two
kinds of violation separately:

- **safety**: something bad happened, for example a lost message or a reply delivered to the wrong request;
- **liveness**: something good never happened, for example a client that didn't recover after the faults were gone.

## What is checked

| Promise (docs) | Workload | Checker | Violation |
|---|---|---|---|
| A completed durable `SendAsync` is delivered **at least once** ([message-durability](../docs/message-durability.md)) | `queue` | `queue` | an acknowledged message was never received (`lost`) |
| Unsettled messages are redelivered when a consumer goes away, not lost ([message-acknowledgment](../docs/message-acknowledgment.md)) | `queue` (consumers sometimes dispose themselves without accepting) | `queue` | `lost` |
| Nothing is delivered that wasn't sent | `queue` | `queue` | `unexpected`, `failed-but-delivered`, `unreadable` (a message without a string body) |
| A request completes only with its own reply ([auto-recovery](../docs/auto-recovery.md#request-reply-during-an-outage)) | `request-reply` | `request-reply` | `crossed-reply`, `phantom-reply` |
| Connections, producers, consumers and request-reply clients recover once the broker is reachable again ([auto-recovery](../docs/auto-recovery.md)) | both | `liveness` | a worker made no progress after the final heal (`no-progress`), an operation never completed (`hung`), or an operation outlived its own timeout (`ignored-cancellation`) |

Duplicates are allowed under at-least-once, so they are counted but never treated as violations. A send that
timed out or failed with an exception is *indeterminate*: the message may or may not have reached the broker, so
the checker accepts it either way.

Not covered yet: transactions, message ordering, and `Send` (fire-and-forget, at-most-once by design).

## Running it

Requires Docker and the .NET 10 SDK. From the repository root:

```bash
docker compose -f chaos/docker-compose.yml up -d
dotnet build -c Release chaos/ArtemisNetClient.Chaos
dotnet run -c Release --no-build --project chaos/ArtemisNetClient.Chaos -- --duration 120
```

The compose file starts its own broker (`artemis-chaos`) and a [Toxiproxy](https://github.com/Shopify/toxiproxy)
instance (`toxiproxy-chaos`) on ports that don't clash with `test/artemis`, so the two can run side by side.

```text
workload clients ──► localhost:25673 ──► Toxiproxy ──► artemis:5672
setup / responder / final drain ──► localhost:25672 ──────► artemis:5672
nemesis ──► Toxiproxy API :8474, Jolokia :28161, docker CLI
```

The workload clients, the subjects of the test, go through the proxy. The setup, the request-reply responder and the
final drain connect to the broker directly.

A run goes through these phases:

1. Toxiproxy is reset and the broker is started if a previous run left it down.
2. A queue unique to the run is declared, so leftovers from earlier runs can't interfere. The `queue` workload turns
   dead-lettering off for it, because faults can use up a message's delivery attempts.
3. The workers start. For `--duration` seconds the nemesis alternates healthy periods (3–8 s) with a random fault
   held for 2–10 s.
4. The final heal removes every fault, then the clients get `--recovery-deadline` seconds to show they recovered.
5. The workers stop. The `queue` workload then drains what's left on the queue through a fresh, direct connection.
6. The checkers run.

### Options

```text
--workload <queue|request-reply>   workload to run (default: queue)
--nemesis <list|all|none>          comma-separated faults (default: all)
--duration <s>                     how long faults are injected (default: 60)
--seed <n>                         seed of the fault schedule (default: random)
--producers <n>                    queue: producers (default: 4)
--consumers <n>                    queue: consumers (default: 2)
--in-flight <n>                    queue: concurrent sends per producer (default: 1)
--requesters <n>                   request-reply: clients (default: 4)
--connections <n>                  connections the workers are spread over (default: 2)
--interval-ms <ms>                 pause between operations of a worker (default: 20)
--op-timeout <s>                   timeout of a single operation (default: 10)
--recovery-deadline <s>            time the clients get to recover after the final heal (default: 30)
--abandon-rate <0..1>              queue: chance a consumer disposes itself without accepting (default: 0.01)
--sabotage                         self-check: hide some received messages, the checker must report them as lost
--out <dir>                        output directory (default: chaos/out)
--host <host>                      host of the docker services (default: $CHAOS_HOST or localhost)
--container <name>                 Artemis container (default: artemis-chaos)
```

`--in-flight` above 1 makes several sends share one producer concurrently, which exercises the concurrent retry
path of the auto-recovering producer. Producers and consumers are spread round-robin over `--connections`
connections, so a single fault can hit several links of one connection at once.

### Faults

| Fault | How | What it exercises |
|---|---|---|
| `refuse` | Toxiproxy proxy disabled | open connections dropped, new ones refused until healed |
| `reset` | `reset_peer` toxic | TCP RST shortly after any data is sent |
| `cut` | `limit_data` toxic | every connection closed after a random number of bytes, usually mid-frame |
| `latency` | `latency` toxic, 100–2000 ms ± 25 % | slow acknowledgements and sends that time out |
| `kill` | `docker kill --signal KILL`, `docker start` on heal | broker crash. The journal survives in the container, so acked durable messages must survive too |
| `restart` | `docker stop`, `docker start` on heal | graceful broker shutdown |
| `broker-close` | Jolokia: `closeConnectionsForUser`, `closeConsumerConnectionsForAddress` or `closeConsumerWithID` | the broker closing a connection or a single consumer link on its own initiative |
| `blackhole` (opt-in) | `timeout` toxic with timeout 0, both directions | stalled connections that are never closed |

`all` means every fault except `blackhole`. `blackhole` stalls traffic without closing anything, so the client has
nothing to react to until the toxic is removed. With holds of at most 10 s, which is shorter than the default op
timeout, the stalled connections simply resume on heal. That makes it a test of stall-and-resume, not of detecting a
dead connection. Note that the client has no AMQP idle timeout setting, only TCP keepalive, and Toxiproxy answers
keepalive probes itself. A half-open connection that never resumes would therefore probably go unnoticed by the
client. Testing that needs a fault that black-holes existing connections permanently while new ones get through.
Toxiproxy can't do that per connection.

## Reading the results

Each run writes to `chaos/out/<run id>/`:

| File | Content |
|---|---|
| `history.jsonl` | every operation, one JSON object per line: `index`, `process`, `type` (`invoke`/`ok`/`fail`/`info`), `f`, `value`, `result`, `error`, `time` (seconds since start) |
| `report.json` | options, seed, verdict, and per checker its counts and anomalies (up to 200 per kind) |
| `client.log` | the library's own logs, timestamped with the history clock |

The console shows the fault timeline and a summary. Each anomaly says what the nemesis was doing at the time, for
example `'p1.0-812' was acknowledged at 44.912s (during 'kill: SIGKILL' (0.3s after injection)) but never received`.

Exit codes: `0` valid, `1` safety violated, `2` liveness violated, `3` unknown (the run was interrupted, the
environment wasn't ready, or the history lacks what a checker needs), `64` bad arguments.

The seed fixes the fault schedule: which faults, in what order, held for how long, and with which parameters.
Rerunning with the same `--seed` replays the same faults. It doesn't replay the interleaving of client operations,
so a violation that depends on timing may need several runs to show up again.

### Process names in the history

| Process | Meaning |
|---|---|
| `p<producer>.<slot>` | a producer slot, sending values `p<producer>.<slot>-<seq>` |
| `c<n>` | a consumer |
| `drain` | the final drain of the `queue` workload |
| `r<n>` | a request-reply client, sending values `r<n>-<seq>` |
| `responder` | the request-reply responder, which echoes every value back |
| `nemesis` | fault injections and heals, plus the `final-heal` and `stop` markers |

### Telling client bugs from broker bugs

A `lost` anomaly means the broker acknowledged a durable message that was later nowhere to be found. If it happened
around a `kill`, check whether the broker lost it (the journal) before blaming the client. The client's part ends
once the broker has accepted the message. The history marks every acknowledgement with its time, and `client.log`
shows what the connection was doing around it.

## Checking the checker

- `dotnet test test/ArtemisNetClient.Chaos.UnitTests` verifies the checkers against synthetic histories, without a
  broker.
- `--sabotage` hides every message whose sequence number is 7 modulo 50 from the history, as if it had never been
  received. The run must end with `SAFETY VIOLATED`, listing those messages as lost. If it doesn't, the pipeline
  can't see a violation, and a valid verdict means nothing.
