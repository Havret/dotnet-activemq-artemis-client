namespace ActiveMQ.Artemis.Client.Chaos;

public interface IWorkload
{
    /// <summary>The queue the workload uses, a target for the broker-close nemesis.</summary>
    string Address { get; }

    Task SetupAsync(CancellationToken ct);

    /// <summary>Starts the workers and returns a task that completes once they have all stopped.</summary>
    Task<Task> StartAsync(CancellationToken stop);

    /// <summary>Runs after the workers stopped, with all faults healed.</summary>
    Task FinishAsync(CancellationToken ct);

    IEnumerable<CheckResult> Check(IReadOnlyList<Op> ops);
}
