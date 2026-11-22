namespace ActiveMQ.Artemis.Client.Chaos;

/// <summary>An invocation and its completion, which is null when the operation never completed.</summary>
public sealed record OpPair(Op Invoke, Op Completion)
{
    public bool IsPending => Completion == null;
}

public static class HistoryAnalysis
{
    /// <summary>
    /// Matches every invoke with the next completion of the same process. A process has at most one outstanding
    /// operation, so the next completion is always its own.
    /// </summary>
    public static IReadOnlyList<OpPair> Pair(IEnumerable<Op> ops)
    {
        var pairs = new List<OpPair>();
        var pending = new Dictionary<string, int>();
        foreach (var op in ops)
        {
            if (op.Type == OpType.Invoke)
            {
                pending[op.Process] = pairs.Count;
                pairs.Add(new OpPair(op, null));
            }
            else if (pending.Remove(op.Process, out var index))
            {
                pairs[index] = pairs[index] with { Completion = op };
            }
        }

        return pairs;
    }

    public static double? TimeOf(IEnumerable<Op> ops, string nemesisValue)
    {
        return ops.FirstOrDefault(x => x.F == Fn.Nemesis && x.Value == nemesisValue && x.Type == OpType.Ok)?.Time;
    }

    /// <summary>
    /// Describes what the nemesis was doing at the given moment, so that an anomaly can be related to a fault.
    /// </summary>
    public static string NemesisContext(IReadOnlyList<Op> ops, double time)
    {
        string active = null;
        double activeSince = 0;
        string last = null;
        double lastHealed = 0;
        foreach (var op in ops.Where(x => x.F == Fn.Nemesis && x.Type == OpType.Ok))
        {
            if (op.Time > time)
            {
                break;
            }

            if (op.Value.StartsWith(NemesisValues.HealPrefix, StringComparison.Ordinal) || op.Value == NemesisValues.FinalHeal)
            {
                if (active != null)
                {
                    last = active;
                    lastHealed = op.Time;
                }

                active = null;
            }
            else if (op.Value != NemesisValues.Stop)
            {
                active = op.Result ?? op.Value;
                activeSince = op.Time;
            }
        }

        if (active != null)
        {
            return $"during '{active}' ({time - activeSince:0.0}s after injection)";
        }

        return last != null
            ? $"{time - lastHealed:0.0}s after '{last}' was healed"
            : "before any fault";
    }
}
