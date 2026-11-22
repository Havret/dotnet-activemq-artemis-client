using ActiveMQ.Artemis.Client.AutoRecovering.RecoveryPolicy;
using ActiveMQ.Artemis.Client.Exceptions;
using ActiveMQ.Artemis.Client.MessageIdPolicy;
using Microsoft.Extensions.Logging;

namespace ActiveMQ.Artemis.Client.Chaos;

public sealed class RunContext
{
    private readonly ConnectionFactory _connectionFactory;

    public RunContext(RunOptions options, string runId, History history, ILoggerFactory loggerFactory)
    {
        Options = options;
        RunId = runId;
        History = history;
        LoggerFactory = loggerFactory;
        ProxyEndpoint = Endpoint.Create(options.Host, options.ProxyPort, options.User, options.Password);
        DirectEndpoint = Endpoint.Create(options.Host, options.DirectPort, options.User, options.Password);

        _connectionFactory = new ConnectionFactory
        {
            LoggerFactory = loggerFactory,
            RecoveryPolicy = RecoveryPolicyFactory.LinearBackoff(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(2)),
            MessageIdPolicyFactory = MessageIdPolicyFactory.GuidMessageIdPolicy,
        };
        _connectionFactory.TCP.KeepAliveTime = 5_000;
        _connectionFactory.TCP.KeepAliveInterval = 1_000;
    }

    public RunOptions Options { get; }
    public string RunId { get; }
    public History History { get; }
    public ILoggerFactory LoggerFactory { get; }

    /// <summary>Goes through Toxiproxy. Used by the workload clients, the subjects of the test.</summary>
    public Endpoint ProxyEndpoint { get; }

    /// <summary>Goes straight to the broker. Used for setup, the responder and the final drain.</summary>
    public Endpoint DirectEndpoint { get; }

    public Task<IConnection> ConnectAsync(Endpoint endpoint, CancellationToken ct) => _connectionFactory.CreateAsync(endpoint, ct);

    public static string Describe(Exception e)
    {
        var description = $"{e.GetType().Name}: {e.Message}";
        if (e is ActiveMQArtemisClientException { ErrorCode: not null } clientException)
        {
            description += $" [{clientException.ErrorCode}]";
        }

        return description;
    }
}
