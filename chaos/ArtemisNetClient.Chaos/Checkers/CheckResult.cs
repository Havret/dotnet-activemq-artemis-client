namespace ActiveMQ.Artemis.Client.Chaos;

public enum Severity
{
    /// <summary>The library broke a promise: something bad happened.</summary>
    Safety,

    /// <summary>The library failed to recover: something good never happened.</summary>
    Liveness,

    /// <summary>Allowed by the promises, but worth knowing about.</summary>
    Info
}

public sealed record Anomaly(Severity Severity, string Kind, string Description);

public sealed class CheckResult
{
    public CheckResult(string checker)
    {
        Checker = checker;
    }

    public string Checker { get; }
    public Dictionary<string, long> Counts { get; } = new();
    public List<Anomaly> Anomalies { get; } = new();

    /// <summary>Set when the history doesn't contain enough information to reach a verdict.</summary>
    public string Unknown { get; set; }

    public bool SafetyViolated => Anomalies.Any(x => x.Severity == Severity.Safety);
    public bool LivenessViolated => Anomalies.Any(x => x.Severity == Severity.Liveness);
    public bool Valid => Unknown == null && !SafetyViolated && !LivenessViolated;

    public void Add(Severity severity, string kind, string description) => Anomalies.Add(new Anomaly(severity, kind, description));
}
