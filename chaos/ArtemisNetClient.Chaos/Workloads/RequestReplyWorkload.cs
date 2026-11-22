using ActiveMQ.Artemis.Client.Exceptions;

namespace ActiveMQ.Artemis.Client.Chaos;

/// <summary>
/// Request-reply clients send requests with unique values through the proxy, a responder connected directly to
/// the broker echoes each value back. The checker verifies that no request ever completes with someone else's reply.
/// </summary>
public sealed class RequestReplyWorkload(RunContext context) : IWorkload
{
    private readonly List<IConnection> _connections = new();
    private readonly List<IAsyncDisposable> _resources = new();
    private RunOptions Options => context.Options;
    private History History => context.History;

    public string Address { get; } = $"chaos-rr-{context.RunId}";

    public async Task SetupAsync(CancellationToken ct)
    {
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

    public async Task<Task> StartAsync(CancellationToken stop)
    {
        var responderConnection = await context.ConnectAsync(context.DirectEndpoint, stop);
        _connections.Add(responderConnection);
        var workers = new List<Task> { Worker.Run(History, "responder", () => RespondAsync(responderConnection, stop)) };

        var clientConnections = new List<IConnection>();
        for (var i = 0; i < Options.Connections; i++)
        {
            clientConnections.Add(await context.ConnectAsync(context.ProxyEndpoint, stop));
        }

        _connections.AddRange(clientConnections);

        for (var i = 0; i < Options.Requesters; i++)
        {
            var process = $"r{i}";
            var client = await clientConnections[i % clientConnections.Count].CreateRequestReplyClientAsync(stop);
            _resources.Add(client);
            workers.Add(Worker.Run(History, process, () => RequestAsync(process, client, stop)));
        }

        return Task.WhenAll(workers);
    }

    private async Task RespondAsync(IConnection connection, CancellationToken stop)
    {
        const string process = "responder";
        await using var consumer = await connection.CreateConsumerAsync(Address, RoutingType.Anycast, stop);
        await using var producer = await connection.CreateAnonymousProducerAsync(stop);
        while (!stop.IsCancellationRequested)
        {
            var request = await consumer.ReceiveAsync(stop);
            var value = request.GetBody<string>();
            History.Record(process, Fn.Respond, OpType.Ok, value);
            try
            {
                await producer.SendAsync(request.ReplyTo, new Message(value) { CorrelationId = request.CorrelationId }, stop);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                // e.g. the requester's reply address is gone because its client was recovered in the meantime
                History.Record(process, Fn.Lifecycle, OpType.Info, $"reply to '{value}' failed", RunContext.Describe(e));
            }

            await consumer.AcceptAsync(request);
        }
    }

    private async Task RequestAsync(string process, IRequestReplyClient client, CancellationToken stop)
    {
        for (long seq = 0; !stop.IsCancellationRequested; seq++)
        {
            var invoke = History.Invoke(process, Fn.Request, $"{process}-{seq}");
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(stop);
            cts.CancelAfter(Options.OperationTimeout);
            try
            {
                var reply = await client.SendAsync(Address, RoutingType.Anycast, new Message(invoke.Value), cts.Token);
                History.Ok(invoke, result: reply.GetBody<string>());
            }
            catch (OperationCanceledException) when (stop.IsCancellationRequested)
            {
                History.Info(invoke, Errors.Shutdown);
                return;
            }
            catch (OperationCanceledException)
            {
                History.Info(invoke, Errors.Timeout);
            }
            catch (ObjectDisposedException e)
            {
                History.Info(invoke, RunContext.Describe(e));
                History.Record(process, Fn.Lifecycle, OpType.Info, "request-reply client terminated", RunContext.Describe(e));
                return;
            }
            catch (Exception e)
            {
                // RequestReplyClientClosedException included: the request was sent, it may have been processed
                History.Info(invoke, RunContext.Describe(e));
            }

            await Worker.Pause(Options.Interval, stop);
        }
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
    }

    public IEnumerable<CheckResult> Check(IReadOnlyList<Op> ops)
    {
        yield return RequestReplyChecker.Check(ops);
        yield return LivenessChecker.Check(ops, Options.OperationTimeout);
    }
}
