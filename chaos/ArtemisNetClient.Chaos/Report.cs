using System.Text.Json;

namespace ActiveMQ.Artemis.Client.Chaos;

public static class Report
{
    public const int ExitValid = 0;
    public const int ExitSafety = 1;
    public const int ExitLiveness = 2;
    public const int ExitUnknown = 3;

    private const int MaxAnomaliesPerKindInReport = 200;
    private const int MaxAnomaliesPerKindOnConsole = 5;

    public static int ExitCode(IReadOnlyList<CheckResult> results)
    {
        if (results.Any(x => x.SafetyViolated)) return ExitSafety;
        if (results.Any(x => x.LivenessViolated)) return ExitLiveness;
        if (results.Any(x => x.Unknown != null)) return ExitUnknown;
        return ExitValid;
    }

    public static string Verdict(int exitCode) => exitCode switch
    {
        ExitValid => "VALID",
        ExitSafety => "SAFETY VIOLATED",
        ExitLiveness => "LIVENESS VIOLATED",
        _ => "UNKNOWN"
    };

    public static async Task WriteAsync(string path, RunOptions options, string runId, IReadOnlyList<CheckResult> results)
    {
        var report = new
        {
            runId,
            verdict = Verdict(ExitCode(results)),
            options.Seed,
            options.Workload,
            faults = options.Faults,
            durationSeconds = options.Duration.TotalSeconds,
            recoveryDeadlineSeconds = options.RecoveryDeadline.TotalSeconds,
            operationTimeoutSeconds = options.OperationTimeout.TotalSeconds,
            options.Producers,
            options.Consumers,
            options.Requesters,
            options.InFlight,
            options.Connections,
            options.Sabotage,
            results = results.Select(x => new
            {
                x.Checker,
                x.Valid,
                x.Unknown,
                x.Counts,
                anomalyCounts = x.Anomalies.GroupBy(a => a.Kind).ToDictionary(g => g.Key, g => g.Count()),
                anomalies = x.Anomalies.GroupBy(a => a.Kind).SelectMany(g => g.Take(MaxAnomaliesPerKindInReport))
            })
        };
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(report, Json.Indented));
    }

    public static void Print(IReadOnlyList<CheckResult> results)
    {
        Console.WriteLine();
        foreach (var result in results)
        {
            var status = result.Unknown != null ? $"UNKNOWN ({result.Unknown})"
                : result.SafetyViolated ? "SAFETY VIOLATED"
                : result.LivenessViolated ? "LIVENESS VIOLATED"
                : "valid";
            Console.WriteLine($"== {result.Checker}: {status}");
            Console.WriteLine("   " + string.Join(", ", result.Counts.Select(x => $"{x.Key}={x.Value}")));
            foreach (var group in result.Anomalies.GroupBy(x => (x.Severity, x.Kind)))
            {
                Console.WriteLine($"   {group.Key.Severity.ToString().ToLowerInvariant()} / {group.Key.Kind}: {group.Count()}");
                foreach (var anomaly in group.Take(MaxAnomaliesPerKindOnConsole))
                {
                    Console.WriteLine($"     - {anomaly.Description}");
                }

                if (group.Count() > MaxAnomaliesPerKindOnConsole)
                {
                    Console.WriteLine($"     ... {group.Count() - MaxAnomaliesPerKindOnConsole} more in report.json");
                }
            }
        }
    }
}
