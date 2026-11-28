using System.Globalization;
using ActiveMQ.Artemis.Client.Chaos;
using Microsoft.Extensions.Logging;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

RunOptions options;
try
{
    options = RunOptions.Parse(args);
}
catch (Exception e) when (e is ArgumentException or FormatException)
{
    Console.Error.WriteLine(e.Message);
    Console.Error.WriteLine(RunOptions.Usage);
    return 64;
}

var runId = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
var runDirectory = Path.GetFullPath(Path.Combine(options.OutputDirectory, runId));
Directory.CreateDirectory(runDirectory);

using var interrupted = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    interrupted.Cancel();
};

await using var history = new History(Path.Combine(runDirectory, "history.jsonl"));
using var loggerProvider = new FileLoggerProvider(Path.Combine(runDirectory, "client.log"), () => history.Now, LogLevel.Information);
using var loggerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Information).AddProvider(loggerProvider));
var context = new RunContext(options, runId, history, loggerFactory);

Console.WriteLine($"run:       {runId}");
Console.WriteLine($"output:    {runDirectory}");
Console.WriteLine($"workload:  {options.Workload}");
Console.WriteLine($"nemesis:   {(options.Faults.Length == 0 ? "none" : string.Join(",", options.Faults))}");
Console.WriteLine($"duration:  {options.Duration.TotalSeconds:0}s + {options.RecoveryDeadline.TotalSeconds:0}s recovery deadline");
Console.WriteLine($"seed:      {options.Seed}");
if (options.Sabotage)
{
    Console.WriteLine("sabotage:  ON, the queue checker must report lost messages");
}

using var toxiproxy = new ToxiproxyClient(options.ToxiproxyApi);
using var jolokia = new JolokiaClient(options.Console, options.User, options.Password, options.BrokerName);
var broker = new BrokerControl(context);

Task HealAll(CancellationToken ct) => Task.WhenAll(toxiproxy.ResetAsync(ct), broker.StartAsync(ct));

try
{
    await toxiproxy.GetVersionAsync(interrupted.Token);
    await broker.StartAsync(interrupted.Token);
}
catch (Exception e) when (!interrupted.IsCancellationRequested)
{
    Console.Error.WriteLine($"The chaos environment isn't ready ({RunContext.Describe(e)}).");
    Console.Error.WriteLine("Start it with: docker compose -f chaos/docker-compose.yml up -d");
    return Report.ExitUnknown;
}

await toxiproxy.ResetAsync(interrupted.Token);
await toxiproxy.RecreateProxyAsync(ToxiproxyFaults.Proxy, $"0.0.0.0:{options.ProxyPort}", options.ProxyUpstream, interrupted.Token);

IWorkload workload = options.Workload == "queue" ? new QueueWorkload(context, jolokia) : new RequestReplyWorkload(context);
await workload.SetupAsync(interrupted.Token);

var faults = options.Faults.Select(IFault (name) => name switch
{
    "refuse" => new ToxiproxyFaults.Refuse(toxiproxy),
    "reset" => new ToxiproxyFaults.Reset(toxiproxy),
    "cut" => new ToxiproxyFaults.Cut(toxiproxy),
    "latency" => new ToxiproxyFaults.Latency(toxiproxy),
    "blackhole" => new ToxiproxyFaults.Blackhole(toxiproxy),
    "kill" or "restart" => new BrokerProcessFault(name, broker),
    "broker-close" => new BrokerCloseFault(jolokia, context, workload.Address),
    _ => throw new ArgumentOutOfRangeException(nameof(name), name, null)
}).ToList();

using var stop = new CancellationTokenSource();
try
{
    var workers = await workload.StartAsync(stop.Token);
    Console.WriteLine($"[{history.Now,7:0.0}s] workers started");

    await new NemesisSchedule(history, faults, HealAll, options.Seed).RunAsync(options.Duration, interrupted.Token);

    Console.WriteLine($"[{history.Now,7:0.0}s] all faults healed, giving the clients {options.RecoveryDeadline.TotalSeconds:0}s to recover");
    await Task.Delay(options.RecoveryDeadline, interrupted.Token);

    history.Record("nemesis", Fn.Nemesis, OpType.Ok, NemesisValues.Stop);
    Console.WriteLine($"[{history.Now,7:0.0}s] stopping workers");
    await stop.CancelAsync();
    if (await Task.WhenAny(workers, Task.Delay(TimeSpan.FromSeconds(30))) != workers)
    {
        Console.WriteLine($"[{history.Now,7:0.0}s] some workers ignored the stop, their pending operations are reported as hung");
    }

    Console.WriteLine($"[{history.Now,7:0.0}s] finishing (draining what's left)");
    using var finishTimeout = CancellationTokenSource.CreateLinkedTokenSource(interrupted.Token);
    finishTimeout.CancelAfter(TimeSpan.FromMinutes(5));
    await workload.FinishAsync(finishTimeout.Token);
}
catch (OperationCanceledException) when (interrupted.IsCancellationRequested)
{
    Console.WriteLine("interrupted, healing everything and exiting without a verdict");
    await stop.CancelAsync();
    await HealAll(CancellationToken.None);
    return Report.ExitUnknown;
}

var ops = history.Snapshot();
var results = workload.Check(ops).ToList();
var reportPath = Path.Combine(runDirectory, "report.json");
await Report.WriteAsync(reportPath, options, runId, results);
Report.Print(results);

var exitCode = Report.ExitCode(results);
Console.WriteLine();
Console.WriteLine($"verdict: {Report.Verdict(exitCode)} (seed {options.Seed}), see {reportPath}");
return exitCode;
