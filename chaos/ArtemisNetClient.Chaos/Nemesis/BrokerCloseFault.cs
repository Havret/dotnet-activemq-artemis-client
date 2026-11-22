using System.Text.Json;

namespace ActiveMQ.Artemis.Client.Chaos;

/// <summary>
/// Makes the broker close connections or consumers on its own initiative, through the management API.
/// This exercises the paths where the peer, rather than the network, ends a connection or a link.
/// </summary>
public sealed class BrokerCloseFault(JolokiaClient jolokia, RunContext context, string address) : IFault
{
    public string Name => "broker-close";

    public async Task<string> InjectAsync(Random random, CancellationToken ct)
    {
        switch (random.Next(3))
        {
            case 0:
                await jolokia.ExecAsync("closeConnectionsForUser(java.lang.String)", [context.Options.User], ct);
                return $"broker-close: all connections of user '{context.Options.User}'";
            case 1:
                await jolokia.ExecAsync("closeConsumerConnectionsForAddress(java.lang.String)", [address], ct);
                return $"broker-close: connections consuming from '{address}'";
            default:
                return await CloseRandomConsumer(random, ct);
        }
    }

    private async Task<string> CloseRandomConsumer(Random random, CancellationToken ct)
    {
        var json = await jolokia.ExecAsync("listAllConsumersAsJSON()", [], ct);
        using var document = JsonDocument.Parse(json.GetString()!);
        var consumers = document.RootElement.EnumerateArray()
            .Where(x => x.TryGetProperty("queueName", out var queue) && queue.GetString() == address)
            .Select(x => (Session: x.GetProperty("sessionID").GetString(), Consumer: x.GetProperty("consumerID").GetInt64()))
            .OrderBy(x => x.Session, StringComparer.Ordinal).ThenBy(x => x.Consumer)
            .ToArray();
        if (consumers.Length == 0)
        {
            return $"broker-close: no consumer on '{address}' to close";
        }

        var (session, consumer) = consumers[random.Next(consumers.Length)];
        await jolokia.ExecAsync("closeConsumerWithID(java.lang.String,java.lang.String)", [session, consumer.ToString()], ct);
        return $"broker-close: consumer {consumer} of session {session}";
    }

    public Task HealAsync(CancellationToken ct) => Task.CompletedTask;
}
