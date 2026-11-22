namespace ActiveMQ.Artemis.Client.Chaos;

/// <summary>
/// Alternates healthy periods with random faults, drawn from a seeded random generator so that a run's fault
/// sequence can be replayed. Every transition is recorded in the history.
/// </summary>
public sealed class NemesisSchedule(History history, IReadOnlyList<IFault> faults, Func<CancellationToken, Task> healAll, int seed)
{
    private const string Process = "nemesis";

    public async Task RunAsync(TimeSpan duration, CancellationToken ct)
    {
        var random = new Random(seed);
        var end = history.Now + duration.TotalSeconds;
        while (faults.Count > 0 && history.Now < end)
        {
            await Task.Delay(TimeSpan.FromSeconds(3 + random.NextDouble() * 5), ct);
            if (history.Now >= end)
            {
                break;
            }

            var fault = faults[random.Next(faults.Count)];
            var holdFor = TimeSpan.FromSeconds(2 + random.NextDouble() * 8);
            // A fault gets its own generator, so however many numbers it draws (which may depend on the broker's
            // state), the rest of the schedule stays the same for a given seed
            var faultRandom = new Random(random.Next());
            var injected = await Step(fault.Name, () => fault.InjectAsync(faultRandom, ct));
            if (injected)
            {
                await Task.Delay(holdFor, ct);
                await Step(NemesisValues.HealPrefix + fault.Name, async () =>
                {
                    await fault.HealAsync(ct);
                    return null;
                });
            }
        }

        if (faults.Count == 0)
        {
            await Task.Delay(duration, ct);
        }

        await Step(NemesisValues.FinalHeal, async () =>
        {
            await healAll(ct);
            return null;
        });
    }

    private async Task<bool> Step(string name, Func<Task<string>> action)
    {
        var invoke = history.Invoke(Process, Fn.Nemesis, name);
        Console.WriteLine($"[{history.Now,7:0.0}s] nemesis: {name}");
        try
        {
            var description = await action();
            history.Ok(invoke, result: description);
            if (description != null)
            {
                Console.WriteLine($"[{history.Now,7:0.0}s] nemesis:   {description}");
            }

            return true;
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            // A fault that couldn't be injected (or healed) is a harness problem, not a finding; the final heal
            // puts everything back in order
            history.Info(invoke, RunContext.Describe(e));
            Console.WriteLine($"[{history.Now,7:0.0}s] nemesis:   failed: {RunContext.Describe(e)}");
            return false;
        }
    }
}
