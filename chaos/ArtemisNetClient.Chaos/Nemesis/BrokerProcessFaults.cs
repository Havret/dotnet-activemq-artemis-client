namespace ActiveMQ.Artemis.Client.Chaos;

/// <summary>
/// Takes the broker down for the duration of the fault. The container's filesystem, and so the journal,
/// survives both, so every acknowledged durable message must still be there once the broker is back.
/// </summary>
public sealed class BrokerProcessFault(string name, BrokerControl broker) : IFault
{
    public string Name => name;

    public async Task<string> InjectAsync(Random random, CancellationToken ct)
    {
        if (name == "kill")
        {
            await broker.KillAsync(ct);
            return "kill: SIGKILL";
        }

        await broker.StopAsync(ct);
        return "restart: graceful stop";
    }

    public Task HealAsync(CancellationToken ct) => broker.StartAsync(ct);
}

public sealed class BrokerControl(RunContext context)
{
    private string Container => context.Options.Container;

    public Task KillAsync(CancellationToken ct) => DockerCli.RunAsync(ct, "kill", "--signal", "KILL", Container);

    public Task StopAsync(CancellationToken ct) => DockerCli.RunAsync(ct, "stop", "--time", "30", Container);

    public async Task StartAsync(CancellationToken ct)
    {
        if (!await DockerCli.IsRunningAsync(Container, ct))
        {
            await DockerCli.RunAsync(ct, "start", Container);
        }

        await WaitUntilReadyAsync(ct);
    }

    /// <summary>Waits until the broker accepts AMQP connections, which happens well after the container starts.</summary>
    public async Task WaitUntilReadyAsync(CancellationToken ct)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromMinutes(2));
        var factory = new ConnectionFactory { AutomaticRecoveryEnabled = false };
        while (true)
        {
            try
            {
                await using var connection = await factory.CreateAsync(context.DirectEndpoint, cts.Token);
                return;
            }
            catch (Exception) when (!cts.IsCancellationRequested)
            {
                await Task.Delay(250, cts.Token);
            }
        }
    }
}
