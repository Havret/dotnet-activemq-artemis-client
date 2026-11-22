namespace ActiveMQ.Artemis.Client.Chaos;

/// <summary>
/// Network faults injected by Toxiproxy, which sits between the workload clients and the broker.
/// </summary>
public static class ToxiproxyFaults
{
    public const string Proxy = "artemis";
    private const string Toxic = "chaos";

    /// <summary>Disables the proxy: open connections are dropped and new ones are refused.</summary>
    public sealed class Refuse(ToxiproxyClient toxiproxy) : IFault
    {
        public string Name => "refuse";

        public async Task<string> InjectAsync(Random random, CancellationToken ct)
        {
            await toxiproxy.SetEnabledAsync(Proxy, false, ct);
            return "refuse: proxy disabled";
        }

        public Task HealAsync(CancellationToken ct) => toxiproxy.SetEnabledAsync(Proxy, true, ct);
    }

    /// <summary>Resets (TCP RST) every connection as soon as the client sends anything.</summary>
    public sealed class Reset(ToxiproxyClient toxiproxy) : IFault
    {
        public string Name => "reset";

        public async Task<string> InjectAsync(Random random, CancellationToken ct)
        {
            var timeout = random.Next(0, 500);
            await toxiproxy.AddToxicAsync(Proxy, Toxic, "reset_peer", "upstream", new { timeout }, ct);
            return $"reset: reset_peer after {timeout}ms";
        }

        public Task HealAsync(CancellationToken ct) => toxiproxy.RemoveToxicAsync(Proxy, Toxic, ct);
    }

    /// <summary>Closes every connection after a number of bytes, most likely in the middle of a frame.</summary>
    public sealed class Cut(ToxiproxyClient toxiproxy) : IFault
    {
        public string Name => "cut";

        public async Task<string> InjectAsync(Random random, CancellationToken ct)
        {
            var bytes = random.Next(100, 20_000);
            var stream = random.Next(2) == 0 ? "upstream" : "downstream";
            await toxiproxy.AddToxicAsync(Proxy, Toxic, "limit_data", stream, new { bytes }, ct);
            return $"cut: limit_data {bytes}B {stream}";
        }

        public Task HealAsync(CancellationToken ct) => toxiproxy.RemoveToxicAsync(Proxy, Toxic, ct);
    }

    public sealed class Latency(ToxiproxyClient toxiproxy) : IFault
    {
        public string Name => "latency";

        public async Task<string> InjectAsync(Random random, CancellationToken ct)
        {
            var latency = random.Next(100, 2_000);
            var jitter = latency / 4;
            await toxiproxy.AddToxicAsync(Proxy, Toxic, "latency", "downstream", new { latency, jitter }, ct);
            return $"latency: {latency}±{jitter}ms";
        }

        public Task HealAsync(CancellationToken ct) => toxiproxy.RemoveToxicAsync(Proxy, Toxic, ct);
    }

    /// <summary>
    /// Stops all data in both directions without closing the connections, the way a network partition stalls them.
    /// Nothing tells the client that anything is wrong until the toxic is removed.
    /// </summary>
    public sealed class Blackhole(ToxiproxyClient toxiproxy) : IFault
    {
        public string Name => "blackhole";

        public async Task<string> InjectAsync(Random random, CancellationToken ct)
        {
            await toxiproxy.AddToxicAsync(Proxy, Toxic + "-up", "timeout", "upstream", new { timeout = 0 }, ct);
            await toxiproxy.AddToxicAsync(Proxy, Toxic + "-down", "timeout", "downstream", new { timeout = 0 }, ct);
            return "blackhole: all data dropped";
        }

        public async Task HealAsync(CancellationToken ct)
        {
            await toxiproxy.RemoveToxicAsync(Proxy, Toxic + "-up", ct);
            await toxiproxy.RemoveToxicAsync(Proxy, Toxic + "-down", ct);
        }
    }
}
