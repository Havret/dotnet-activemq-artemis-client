namespace ActiveMQ.Artemis.Client.Chaos;

/// <summary>
/// Checks that a request-reply client never completes a request with a reply meant for another request, and never
/// completes a request the responder hasn't seen.
/// </summary>
public static class RequestReplyChecker
{
    public const string Name = "request-reply";

    public static CheckResult Check(IReadOnlyList<Op> ops)
    {
        var result = new CheckResult(Name);
        var responded = ops.Where(x => x.F == Fn.Respond && x.Type == OpType.Ok).Select(x => x.Value).ToHashSet();
        var requests = HistoryAnalysis.Pair(ops.Where(x => x.F == Fn.Request));

        foreach (var (invoke, completion) in requests.Where(x => x.Completion?.Type == OpType.Ok))
        {
            if (completion.Result != invoke.Value)
            {
                result.Add(Severity.Safety, "crossed-reply",
                    $"request '{invoke.Value}' completed at {completion.Time:0.000}s ({HistoryAnalysis.NemesisContext(ops, completion.Time)}) with the reply '{completion.Result}'");
            }
            else if (!responded.Contains(invoke.Value))
            {
                result.Add(Severity.Safety, "phantom-reply", $"request '{invoke.Value}' got a reply although the responder never received it");
            }
        }

        result.Counts["requests"] = requests.Count;
        result.Counts["ok"] = requests.Count(x => x.Completion?.Type == OpType.Ok);
        result.Counts["failed"] = requests.Count(x => x.Completion?.Type == OpType.Fail);
        result.Counts["indeterminate"] = requests.Count(x => x.Completion?.Type == OpType.Info);
        result.Counts["client-closed"] = requests.Count(x => x.Completion?.Error?.StartsWith(nameof(Exceptions.RequestReplyClientClosedException)) == true);
        result.Counts["timeouts"] = requests.Count(x => x.Completion?.Error == Errors.Timeout);
        result.Counts["responded"] = responded.Count;

        if (result.Counts["requests"] == 0)
        {
            result.Unknown = "no request was sent";
        }

        return result;
    }
}
