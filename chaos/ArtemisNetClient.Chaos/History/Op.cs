namespace ActiveMQ.Artemis.Client.Chaos;

public enum OpType
{
    /// <summary>The operation started.</summary>
    Invoke,

    /// <summary>The operation definitely happened.</summary>
    Ok,

    /// <summary>The operation definitely did not happen.</summary>
    Fail,

    /// <summary>The outcome is unknown: the operation may or may not have happened.</summary>
    Info
}

public static class Fn
{
    public const string Send = "send";
    public const string Receive = "receive";
    public const string Drain = "drain";
    public const string Request = "request";
    public const string Respond = "respond";
    public const string Accept = "accept";
    public const string Lifecycle = "lifecycle";
    public const string Nemesis = "nemesis";
}

public static class NemesisValues
{
    public const string FinalHeal = "final-heal";
    public const string Stop = "stop";
    public const string HealPrefix = "heal ";
}

public static class Errors
{
    /// <summary>The operation was interrupted because the run was stopping.</summary>
    public const string Shutdown = "shutdown";

    public const string Timeout = "timeout";
}

/// <summary>
/// A single entry of the history. Every operation is recorded as an <see cref="OpType.Invoke"/> followed by
/// a completion (<see cref="OpType.Ok"/>, <see cref="OpType.Fail"/> or <see cref="OpType.Info"/>) of the same process.
/// </summary>
public sealed record Op
{
    public long Index { get; init; }
    public string Process { get; init; }
    public OpType Type { get; init; }
    public string F { get; init; }
    public string Value { get; init; }
    public string Result { get; init; }
    public uint? DeliveryCount { get; init; }
    public string Error { get; init; }

    /// <summary>Seconds since the start of the run.</summary>
    public double Time { get; init; }

    public bool IsCompletion => Type != OpType.Invoke;
}
