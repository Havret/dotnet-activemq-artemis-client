using System.Globalization;
using ActiveMQ.Artemis.Client.Exceptions;

namespace ActiveMQ.Artemis.Client.Chaos;

/// <summary>
/// Producers send durable messages with unique values to a single anycast queue, consumers receive and accept
/// them. After the run a drain collects whatever is left on the queue, and the checker verifies that every
/// acknowledged message was received at least once.
/// </summary>
public sealed class QueueWorkload(RunContext context, JolokiaClient jolokia) : IWorkload
{
    private readonly List<IConnection> _connections = new();
    private readonly List<IAsyncDisposable> _resources = new();
    private RunOptions Options => context.Options;
    private History History => context.History;

    public string Address { get; } = $"chaos-queue-{context.RunId}";

    public async Task SetupAsync(CancellationToken ct)
    {
        await DisableDeadLetteringAsync(ct);
        await using var connection = await context.ConnectAsync(context.DirectEndpoint, ct);
        await using var topologyManager = await connection.CreateTopologyManagerAsync(ct);
        await topologyManager.DeclareQueueAsync(new QueueConfiguration
        {
            Name = Address,
            Address = Address,
            RoutingType = RoutingType.Anycast,
            AutoCreateAddress = true,
            Durable = true
        }, ct);
    }

    /// <summary>
    /// A fault can take a consumer down many times while it holds the same message. By default the broker moves such a
    /// message to the dead letter queue after a number of delivery attempts, where the checker would take it for lost.
    /// </summary>
    private async Task DisableDeadLetteringAsync(CancellationToken ct)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromMinutes(1));
        while (true)
        {
            try
            {
                await jolokia.ExecAsync("addAddressSettings(java.lang.String,java.lang.String)", [Address, "{\"maxDeliveryAttempts\":-1}"], cts.Token);
                return;
            }
            // The management API can come up a moment after the broker starts accepting AMQP connections
            catch (HttpRequestException) when (!cts.IsCancellationRequested)
            {
                await Task.Delay(250, cts.Token);
            }
        }
    }

    public async Task<Task> StartAsync(CancellationToken stop)
    {
        for (var i = 0; i < Options.Connections; i++)
        {
            _connections.Add(await context.ConnectAsync(context.ProxyEndpoint, stop));
        }

        var workers = new List<Task>();
        for (var i = 0; i < Options.Producers; i++)
        {
            var producer = await ConnectionFor(i).CreateProducerAsync(new ProducerConfiguration
            {
                Address = Address,
                RoutingType = RoutingType.Anycast,
                MessageDurabilityMode = DurabilityMode.Durable
            }, stop);
            _resources.Add(producer);
            for (var slot = 0; slot < Options.InFlight; slot++)
            {
                var process = $"p{i}.{slot}";
                workers.Add(Worker.Run(History, process, () => ProduceAsync(process, producer, stop)));
            }
        }

        for (var i = 0; i < Options.Consumers; i++)
        {
            var process = $"c{i}";
            var connection = ConnectionFor(Options.Producers + i);
            var random = new Random(HashCode.Combine(Options.Seed, i));
            workers.Add(Worker.Run(History, process, () => ConsumeAsync(process, connection, random, stop)));
        }

        return Task.WhenAll(workers);
    }

    private IConnection ConnectionFor(int worker) => _connections[worker % _connections.Count];

    private async Task ProduceAsync(string process, IProducer producer, CancellationToken stop)
    {
        for (long seq = 0; !stop.IsCancellationRequested; seq++)
        {
            var invoke = History.Invoke(process, Fn.Send, $"{process}-{seq}");
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(stop);
            cts.CancelAfter(Options.OperationTimeout);
            try
            {
                await producer.SendAsync(new Message(invoke.Value), cts.Token);
                History.Ok(invoke);
            }
            catch (OperationCanceledException) when (stop.IsCancellationRequested)
            {
                History.Info(invoke, Errors.Shutdown);
                return;
            }
            catch (OperationCanceledException)
            {
                // The message may have reached the broker before the cancellation
                History.Info(invoke, Errors.Timeout);
            }
            catch (Exception e) when (e is ProducerClosedException or ObjectDisposedException)
            {
                // The auto-recovering producer retries recoverable failures itself, so this is terminal
                History.Info(invoke, RunContext.Describe(e));
                History.Record(process, Fn.Lifecycle, OpType.Info, "producer terminated", RunContext.Describe(e));
                return;
            }
            catch (Exception e)
            {
                History.Info(invoke, RunContext.Describe(e));
            }

            await Worker.Pause(Options.Interval, stop);
        }
    }

    private async Task ConsumeAsync(string process, IConnection connection, Random random, CancellationToken stop)
    {
        var consumer = await CreateConsumer(connection, stop);
        try
        {
            while (!stop.IsCancellationRequested)
            {
                var invoke = History.Invoke(process, Fn.Receive);
                Message message;
                using (var cts = CancellationTokenSource.CreateLinkedTokenSource(stop))
                {
                    cts.CancelAfter(Options.OperationTimeout);
                    try
                    {
                        message = await consumer.ReceiveAsync(cts.Token);
                    }
                    catch (OperationCanceledException) when (stop.IsCancellationRequested)
                    {
                        History.Fail(invoke, Errors.Shutdown);
                        return;
                    }
                    catch (OperationCanceledException)
                    {
                        History.Fail(invoke, Errors.Timeout);
                        continue;
                    }
                    catch (Exception e) when (e is ConsumerClosedException or ObjectDisposedException)
                    {
                        History.Fail(invoke, RunContext.Describe(e));
                        History.Record(process, Fn.Lifecycle, OpType.Info, "consumer terminated", RunContext.Describe(e));
                        return;
                    }
                    catch (Exception e)
                    {
                        History.Fail(invoke, RunContext.Describe(e));
                        await Worker.Pause(TimeSpan.FromMilliseconds(100), stop);
                        continue;
                    }
                }

                var value = RecordReceived(invoke, message);

                if (random.NextDouble() < Options.AbandonRate)
                {
                    // Walk away without settling: the message, and anything else buffered, must be redelivered
                    History.Record(process, Fn.Lifecycle, OpType.Ok, $"abandoned consumer holding '{value}'");
                    await Worker.DisposeQuietly(consumer);
                    consumer = await CreateConsumer(connection, stop);
                    continue;
                }

                try
                {
                    await consumer.AcceptAsync(message);
                }
                catch (Exception e)
                {
                    History.Record(process, Fn.Accept, OpType.Info, value, RunContext.Describe(e));
                }
            }
        }
        finally
        {
            await Worker.DisposeQuietly(consumer);
        }
    }

    private Task<IConsumer> CreateConsumer(IConnection connection, CancellationToken ct)
    {
        return connection.CreateConsumerAsync(new ConsumerConfiguration { Address = Address, RoutingType = RoutingType.Anycast, Credit = 50 }, ct);
    }

    private string RecordReceived(Op invoke, Message message)
    {
        // Null if the body isn't a string, which no producer of this workload sends. The checker reports it.
        var value = message.GetBody<string>();
        if (value != null && Sabotaged(value))
        {
            History.Fail(invoke, "sabotage");
        }
        else
        {
            var details = value == null ? $"message-id: '{message.MessageId}', creation-time: '{message.CreationTime:O}'" : null;
            History.Ok(invoke, value, details, message.DeliveryCount);
        }

        return value;
    }

    /// <summary>
    /// Self-check of the harness: hides every 50th message of each producer from the history as if it had never
    /// been received. The checker must report those as lost.
    /// </summary>
    private bool Sabotaged(string value)
    {
        return Options.Sabotage && long.Parse(value[(value.LastIndexOf('-') + 1)..], CultureInfo.InvariantCulture) % 50 == 7;
    }

    public async Task FinishAsync(CancellationToken ct)
    {
        foreach (var resource in _resources)
        {
            await Worker.DisposeQuietly(resource);
        }

        foreach (var connection in _connections)
        {
            await Worker.DisposeQuietly(connection);
        }

        await DrainAsync(ct);
    }

    private async Task DrainAsync(CancellationToken ct)
    {
        const string process = "drain";
        var idleTimeout = TimeSpan.FromSeconds(5);
        await using var connection = await context.ConnectAsync(context.DirectEndpoint, ct);
        await using var consumer = await connection.CreateConsumerAsync(Address, RoutingType.Anycast, ct);
        while (true)
        {
            var invoke = History.Invoke(process, Fn.Drain);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(idleTimeout);
            Message message;
            try
            {
                message = await consumer.ReceiveAsync(cts.Token);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                History.Fail(invoke, "empty");
                return;
            }

            RecordReceived(invoke, message);
            await consumer.AcceptAsync(message);
        }
    }

    public IEnumerable<CheckResult> Check(IReadOnlyList<Op> ops)
    {
        yield return QueueChecker.Check(ops);
        yield return LivenessChecker.Check(ops, Options.OperationTimeout);
    }
}
