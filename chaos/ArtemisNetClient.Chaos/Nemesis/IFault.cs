namespace ActiveMQ.Artemis.Client.Chaos;

public interface IFault
{
    string Name { get; }

    /// <summary>Injects the fault and returns a description of what exactly was done.</summary>
    Task<string> InjectAsync(Random random, CancellationToken ct);

    Task HealAsync(CancellationToken ct);
}
