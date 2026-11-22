using System.Globalization;

namespace ActiveMQ.Artemis.Client.Chaos;

public sealed class RunOptions
{
    /// <summary>Faults that are safe to combine and that the library is expected to survive.</summary>
    public static readonly string[] AllFaults = ["refuse", "reset", "cut", "latency", "kill", "restart", "broker-close"];

    /// <summary>Faults that are only run when named explicitly.</summary>
    public static readonly string[] OptInFaults = ["blackhole"];

    public string Workload { get; private set; } = "queue";
    public string[] Faults { get; private set; } = AllFaults;
    public TimeSpan Duration { get; private set; } = TimeSpan.FromSeconds(60);
    public int Seed { get; private set; } = Random.Shared.Next();
    public int Producers { get; private set; } = 4;
    public int Consumers { get; private set; } = 2;
    public int Requesters { get; private set; } = 4;
    public int InFlight { get; private set; } = 1;
    public int Connections { get; private set; } = 2;
    public TimeSpan Interval { get; private set; } = TimeSpan.FromMilliseconds(20);
    public TimeSpan OperationTimeout { get; private set; } = TimeSpan.FromSeconds(10);
    public TimeSpan RecoveryDeadline { get; private set; } = TimeSpan.FromSeconds(30);
    public double AbandonRate { get; private set; } = 0.01;
    public bool Sabotage { get; private set; }
    public string OutputDirectory { get; private set; } = Path.Combine("chaos", "out");

    public string Host { get; private set; } = Environment.GetEnvironmentVariable("CHAOS_HOST") ?? "localhost";
    public int ProxyPort { get; private set; } = 25673;
    public int DirectPort { get; private set; } = 25672;
    public string ProxyUpstream { get; private set; } = "artemis:5672";
    public Uri ToxiproxyApi { get; private set; }
    public Uri Console { get; private set; }
    public string Container { get; private set; } = "artemis-chaos";
    public string User { get; private set; } = Environment.GetEnvironmentVariable("ARTEMIS_USERNAME") ?? "artemis";
    public string Password { get; private set; } = Environment.GetEnvironmentVariable("ARTEMIS_PASSWORD") ?? "artemis";
    public string BrokerName { get; private set; } = "0.0.0.0";

    public const string Usage = """
        Usage: dotnet run -c Release --project chaos/ArtemisNetClient.Chaos -- [options]

          --workload <queue|request-reply>   workload to run (default: queue)
          --nemesis <list|all|none>          comma-separated faults (default: all)
                                             all = refuse,reset,cut,latency,kill,restart,broker-close
                                             opt-in: blackhole
          --duration <s>                     how long faults are injected (default: 60)
          --seed <n>                         seed of the fault schedule (default: random)
          --producers <n>                    queue: producers (default: 4)
          --consumers <n>                    queue: consumers (default: 2)
          --in-flight <n>                    queue: concurrent sends per producer (default: 1)
          --requesters <n>                   request-reply: clients (default: 4)
          --connections <n>                  connections the workers are spread over (default: 2)
          --interval-ms <ms>                 pause between operations of a worker (default: 20)
          --op-timeout <s>                   timeout of a single operation (default: 10)
          --recovery-deadline <s>            time the clients get to recover after the final heal (default: 30)
          --abandon-rate <0..1>              queue: chance a consumer disposes itself without accepting (default: 0.01)
          --sabotage                         self-check: hide some received messages, the checker must report them as lost
          --out <dir>                        output directory (default: chaos/out)
          --host <host>                      host of the docker services (default: $CHAOS_HOST or localhost)
          --container <name>                 Artemis container (default: artemis-chaos)
        """;

    public static RunOptions Parse(string[] args)
    {
        var options = new RunOptions();
        for (var i = 0; i < args.Length; i++)
        {
            var name = args[i];
            string Next() => i + 1 < args.Length ? args[++i] : throw new ArgumentException($"Missing value for {name}");
            int NextInt() => int.Parse(Next(), CultureInfo.InvariantCulture);

            switch (name)
            {
                case "--workload": options.Workload = Next(); break;
                case "--nemesis": options.Faults = ParseFaults(Next()); break;
                case "--duration": options.Duration = TimeSpan.FromSeconds(NextInt()); break;
                case "--seed": options.Seed = NextInt(); break;
                case "--producers": options.Producers = NextInt(); break;
                case "--consumers": options.Consumers = NextInt(); break;
                case "--in-flight": options.InFlight = NextInt(); break;
                case "--requesters": options.Requesters = NextInt(); break;
                case "--connections": options.Connections = NextInt(); break;
                case "--interval-ms": options.Interval = TimeSpan.FromMilliseconds(NextInt()); break;
                case "--op-timeout": options.OperationTimeout = TimeSpan.FromSeconds(NextInt()); break;
                case "--recovery-deadline": options.RecoveryDeadline = TimeSpan.FromSeconds(NextInt()); break;
                case "--abandon-rate": options.AbandonRate = double.Parse(Next(), CultureInfo.InvariantCulture); break;
                case "--sabotage": options.Sabotage = true; break;
                case "--out": options.OutputDirectory = Next(); break;
                case "--host": options.Host = Next(); break;
                case "--container": options.Container = Next(); break;
                default: throw new ArgumentException($"Unknown option {name}");
            }
        }

        if (options.Workload is not ("queue" or "request-reply"))
        {
            throw new ArgumentException($"Unknown workload {options.Workload}");
        }

        if (options.Connections < 1 || options.InFlight < 1)
        {
            throw new ArgumentException("--connections and --in-flight must be at least 1");
        }

        options.ToxiproxyApi = new Uri($"http://{options.Host}:8474/");
        options.Console = new Uri($"http://{options.Host}:28161/");
        return options;
    }

    private static string[] ParseFaults(string value)
    {
        if (value == "none")
        {
            return [];
        }

        if (value == "all")
        {
            return AllFaults;
        }

        var faults = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Distinct().ToArray();
        var unknown = faults.Except(AllFaults).Except(OptInFaults).ToArray();
        if (unknown.Length > 0)
        {
            throw new ArgumentException($"Unknown faults: {string.Join(", ", unknown)}");
        }

        return faults;
    }
}
