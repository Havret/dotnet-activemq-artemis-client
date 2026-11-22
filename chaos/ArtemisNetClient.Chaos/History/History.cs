using System.Diagnostics;
using System.Text.Json;
using System.Threading.Channels;

namespace ActiveMQ.Artemis.Client.Chaos;

/// <summary>
/// Append-only, thread-safe record of everything the clients and the nemesis did. Kept in memory for the checkers
/// and streamed to a JSONL file so a run can be inspected after the fact (or after a crash).
/// </summary>
public sealed class History : IAsyncDisposable
{
    private readonly object _lock = new();
    private readonly List<Op> _ops = new();
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private readonly Channel<Op> _channel = Channel.CreateUnbounded<Op>(new UnboundedChannelOptions { SingleReader = true });
    private readonly Task _writer;
    private long _index;

    public History(string path)
    {
        _writer = WriteAsync(path);
    }

    public double Now => _clock.Elapsed.TotalSeconds;

    public Op Invoke(string process, string f, string value = null)
    {
        return Append(new Op { Process = process, Type = OpType.Invoke, F = f, Value = value });
    }

    public Op Ok(Op invoke, string value = null, string result = null, uint? deliveryCount = null)
    {
        return Append(new Op
        {
            Process = invoke.Process,
            Type = OpType.Ok,
            F = invoke.F,
            Value = value ?? invoke.Value,
            Result = result,
            DeliveryCount = deliveryCount
        });
    }

    public Op Fail(Op invoke, string error) => Complete(invoke, OpType.Fail, error);

    public Op Info(Op invoke, string error) => Complete(invoke, OpType.Info, error);

    /// <summary>
    /// Records a complete operation as an invoke immediately followed by its completion.
    /// </summary>
    public void Record(string process, string f, OpType type, string value, string error = null, string result = null)
    {
        Invoke(process, f, value);
        Append(new Op { Process = process, Type = type, F = f, Value = value, Error = error, Result = result });
    }

    public IReadOnlyList<Op> Snapshot()
    {
        lock (_lock)
        {
            return _ops.ToArray();
        }
    }

    private Op Complete(Op invoke, OpType type, string error)
    {
        return Append(new Op { Process = invoke.Process, Type = type, F = invoke.F, Value = invoke.Value, Error = error });
    }

    private Op Append(Op op)
    {
        lock (_lock)
        {
            op = op with { Index = _index++, Time = Math.Round(Now, 4) };
            _ops.Add(op);
            // Inside the lock, so that the file is ordered the same way as the in-memory history
            _channel.Writer.TryWrite(op);
            return op;
        }
    }

    private async Task WriteAsync(string path)
    {
        await using var stream = File.Create(path);
        await using var writer = new StreamWriter(stream);
        await foreach (var op in _channel.Reader.ReadAllAsync())
        {
            await writer.WriteLineAsync(JsonSerializer.Serialize(op, Json.Options));
        }
    }

    public async ValueTask DisposeAsync()
    {
        _channel.Writer.TryComplete();
        await _writer;
    }
}
