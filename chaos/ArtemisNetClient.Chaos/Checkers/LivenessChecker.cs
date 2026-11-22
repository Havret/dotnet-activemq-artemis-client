namespace ActiveMQ.Artemis.Client.Chaos;

/// <summary>
/// Checks that the clients recover once the faults are gone: every worker must complete an operation successfully
/// between the final heal and the stop, no operation may stay pending until the stop, and no operation may
/// outlive its own timeout (which would mean the cancellation was ignored).
/// </summary>
public static class LivenessChecker
{
    public const string Name = "liveness";

    private static readonly HashSet<string> WorkerFunctions = [Fn.Send, Fn.Receive, Fn.Request];

    // Time a cancelled operation gets to unwind before it counts as ignoring its cancellation
    private const double CancellationGrace = 5;

    public static CheckResult Check(IReadOnlyList<Op> ops, TimeSpan operationTimeout)
    {
        var result = new CheckResult(Name);
        var finalHeal = HistoryAnalysis.TimeOf(ops, NemesisValues.FinalHeal);
        var stop = HistoryAnalysis.TimeOf(ops, NemesisValues.Stop);
        if (finalHeal == null || stop == null)
        {
            result.Unknown = "the run did not reach the final heal and stop";
            return result;
        }

        var workerOps = ops.Where(x => WorkerFunctions.Contains(x.F)).ToArray();
        var pairs = HistoryAnalysis.Pair(workerOps);

        foreach (var process in workerOps.Select(x => x.Process).Distinct().OrderBy(x => x, StringComparer.Ordinal))
        {
            var progressed = workerOps.Any(x => x.Process == process && x.Type == OpType.Ok && x.Time >= finalHeal && x.Time <= stop);
            if (!progressed)
            {
                var last = workerOps.LastOrDefault(x => x.Process == process && x.Type == OpType.Ok);
                result.Add(Severity.Liveness, "no-progress",
                    $"'{process}' completed nothing in the {stop - finalHeal:0.0}s between the final heal and the stop " +
                    $"(last success: {(last == null ? "never" : $"{last.Time:0.000}s")})");
            }
        }

        foreach (var (invoke, completion) in pairs)
        {
            if (completion == null)
            {
                result.Add(Severity.Liveness, "hung",
                    $"'{invoke.Process}' {invoke.F} invoked at {invoke.Time:0.000}s ({HistoryAnalysis.NemesisContext(ops, invoke.Time)}) never completed, not even after the stop");
            }
            else if (completion.Time - invoke.Time > operationTimeout.TotalSeconds + CancellationGrace)
            {
                result.Add(Severity.Liveness, "ignored-cancellation",
                    $"'{invoke.Process}' {invoke.F} invoked at {invoke.Time:0.000}s took {completion.Time - invoke.Time:0.0}s although its timeout is {operationTimeout.TotalSeconds:0}s");
            }
        }

        result.Counts["workers"] = workerOps.Select(x => x.Process).Distinct().Count();
        result.Counts["ops"] = pairs.Count;
        result.Counts["ok-after-final-heal"] = workerOps.Count(x => x.Type == OpType.Ok && x.Time >= finalHeal && x.Time <= stop);
        result.Counts["library-lifecycle-events"] = ops.Count(x => x.F == Fn.Lifecycle && x.Type == OpType.Info);

        foreach (var lifecycle in ops.Where(x => x.F == Fn.Lifecycle && x.Type == OpType.Info))
        {
            result.Add(Severity.Info, "lifecycle", $"'{lifecycle.Process}' at {lifecycle.Time:0.000}s: {lifecycle.Value} {lifecycle.Error}".TrimEnd());
        }

        return result;
    }
}
