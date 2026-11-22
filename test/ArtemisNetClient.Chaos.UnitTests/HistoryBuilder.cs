using System.Collections.Generic;

namespace ActiveMQ.Artemis.Client.Chaos.UnitTests;

/// <summary>
/// Builds synthetic histories with an explicit clock, so the checkers can be verified without a broker.
/// </summary>
public class HistoryBuilder
{
    private readonly List<Op> _ops = new();

    public double Time { get; private set; }

    public HistoryBuilder At(double time)
    {
        Time = time;
        return this;
    }

    public HistoryBuilder Add(string process, OpType type, string f, string value = null, string result = null, string error = null, uint? deliveryCount = null)
    {
        _ops.Add(new Op
        {
            Index = _ops.Count,
            Process = process,
            Type = type,
            F = f,
            Value = value,
            Result = result,
            Error = error,
            DeliveryCount = deliveryCount,
            Time = Time
        });
        return this;
    }

    public HistoryBuilder Op(string process, string f, string value, OpType outcome, string result = null, string error = null)
    {
        return Add(process, OpType.Invoke, f, value).Add(process, outcome, f, value, result, error);
    }

    public HistoryBuilder Sent(string value) => Op("p0", Fn.Send, value, OpType.Ok);

    public HistoryBuilder Received(string value, string process = "c0", uint deliveryCount = 0)
    {
        return Add(process, OpType.Invoke, Fn.Receive).Add(process, OpType.Ok, Fn.Receive, value, deliveryCount: deliveryCount);
    }

    public HistoryBuilder Nemesis(string value, string result = null) => Op("nemesis", Fn.Nemesis, value, OpType.Ok, result);

    public IReadOnlyList<Op> Build() => _ops.ToArray();
}
