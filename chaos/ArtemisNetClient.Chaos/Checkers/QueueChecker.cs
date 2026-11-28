namespace ActiveMQ.Artemis.Client.Chaos;

/// <summary>
/// Checks the at-least-once promise of durable <c>SendAsync</c>: every acknowledged message must be received
/// (by a workload consumer or by the final drain), and nothing may be received that was never sent.
/// Duplicates are allowed and only counted.
/// </summary>
public static class QueueChecker
{
    public const string Name = "queue";

    public static CheckResult Check(IReadOnlyList<Op> ops)
    {
        var result = new CheckResult(Name);

        var attempted = Values(ops, Fn.Send, OpType.Invoke);
        var acked = Values(ops, Fn.Send, OpType.Ok);
        var failed = Values(ops, Fn.Send, OpType.Fail);
        var indeterminate = Values(ops, Fn.Send, OpType.Info);
        var deliveries = ops.Where(x => x.Type == OpType.Ok && x.F is Fn.Receive or Fn.Drain).ToArray();
        var received = deliveries
            .Where(x => x.Value != null)
            .GroupBy(x => x.Value)
            .ToDictionary(x => x.Key, x => x.Count());

        var ackTimes = ops
            .Where(x => x.F == Fn.Send && x.Type == OpType.Ok)
            .GroupBy(x => x.Value)
            .ToDictionary(x => x.Key, x => x.First().Time);
        foreach (var value in acked.Where(x => !received.ContainsKey(x)).OrderBy(x => ackTimes[x]))
        {
            var time = ackTimes[value];
            result.Add(Severity.Safety, "lost",
                $"'{value}' was acknowledged at {time:0.000}s ({HistoryAnalysis.NemesisContext(ops, time)}) but never received");
        }

        foreach (var delivery in deliveries.Where(x => x.Value == null))
        {
            result.Add(Severity.Safety, "unreadable",
                $"'{delivery.Process}' received a message without a string body at {delivery.Time:0.000}s ({HistoryAnalysis.NemesisContext(ops, delivery.Time)}), delivery count {delivery.DeliveryCount}, {delivery.Result}");
        }

        foreach (var value in received.Keys.Where(x => !attempted.Contains(x)))
        {
            result.Add(Severity.Safety, "unexpected", $"'{value}' was received but never sent");
        }

        foreach (var value in received.Keys.Where(failed.Contains))
        {
            result.Add(Severity.Safety, "failed-but-delivered", $"'{value}' was received although its send definitely failed");
        }

        var duplicates = received.Where(x => x.Value > 1).ToArray();
        var recovered = indeterminate.Count(received.ContainsKey);
        var redelivered = deliveries.Count(x => x.DeliveryCount > 0);

        result.Counts["attempted"] = attempted.Count;
        result.Counts["acknowledged"] = acked.Count;
        result.Counts["failed"] = failed.Count;
        result.Counts["indeterminate"] = indeterminate.Count;
        result.Counts["indeterminate-received"] = recovered;
        result.Counts["received-distinct"] = received.Count;
        result.Counts["received-by-drain"] = ops.Count(x => x.Type == OpType.Ok && x.F == Fn.Drain);
        result.Counts["duplicated"] = duplicates.Length;
        result.Counts["duplicate-deliveries"] = duplicates.Sum(x => x.Value - 1);
        result.Counts["redelivered"] = redelivered;
        result.Counts["lost"] = result.Anomalies.Count(x => x.Kind == "lost");
        result.Counts["unreadable"] = result.Anomalies.Count(x => x.Kind == "unreadable");

        if (attempted.Count == 0)
        {
            result.Unknown = "no message was sent";
        }

        return result;
    }

    private static HashSet<string> Values(IReadOnlyList<Op> ops, string f, OpType type)
    {
        return ops.Where(x => x.F == f && x.Type == type).Select(x => x.Value).ToHashSet();
    }
}
