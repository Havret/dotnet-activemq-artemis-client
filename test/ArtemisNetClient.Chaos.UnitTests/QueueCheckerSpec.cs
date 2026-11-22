using Xunit;

namespace ActiveMQ.Artemis.Client.Chaos.UnitTests;

public class QueueCheckerSpec
{
    [Fact]
    public void Should_accept_history_where_every_acknowledged_message_was_received()
    {
        var ops = new HistoryBuilder()
            .Sent("p0-0")
            .Sent("p0-1")
            .Received("p0-0")
            .Add("drain", OpType.Invoke, Fn.Drain).Add("drain", OpType.Ok, Fn.Drain, "p0-1")
            .Build();

        var result = QueueChecker.Check(ops);

        Assert.True(result.Valid);
        Assert.Equal(2, result.Counts["acknowledged"]);
        Assert.Equal(1, result.Counts["received-by-drain"]);
    }

    [Fact]
    public void Should_report_acknowledged_message_that_was_never_received_as_lost()
    {
        var ops = new HistoryBuilder()
            .Nemesis("kill", "kill: SIGKILL")
            .At(1).Sent("p0-0")
            .Sent("p0-1")
            .Received("p0-1")
            .Build();

        var result = QueueChecker.Check(ops);

        Assert.True(result.SafetyViolated);
        var anomaly = Assert.Single(result.Anomalies);
        Assert.Equal("lost", anomaly.Kind);
        Assert.Contains("'p0-0'", anomaly.Description);
        Assert.Contains("during 'kill: SIGKILL'", anomaly.Description);
    }

    [Fact]
    public void Should_report_message_that_was_never_sent_as_unexpected()
    {
        var ops = new HistoryBuilder()
            .Sent("p0-0")
            .Received("p0-0")
            .Received("p9-9")
            .Build();

        var result = QueueChecker.Check(ops);

        Assert.True(result.SafetyViolated);
        Assert.Equal("unexpected", Assert.Single(result.Anomalies).Kind);
    }

    [Fact]
    public void Should_report_message_whose_send_definitely_failed_but_was_received()
    {
        var ops = new HistoryBuilder()
            .Op("p0", Fn.Send, "p0-0", OpType.Fail, error: "rejected")
            .Received("p0-0")
            .Build();

        var result = QueueChecker.Check(ops);

        Assert.True(result.SafetyViolated);
        Assert.Equal("failed-but-delivered", Assert.Single(result.Anomalies).Kind);
    }

    [Fact]
    public void Should_allow_duplicates()
    {
        var ops = new HistoryBuilder()
            .Sent("p0-0")
            .Received("p0-0")
            .Received("p0-0", "c1", deliveryCount: 1)
            .Build();

        var result = QueueChecker.Check(ops);

        Assert.True(result.Valid);
        Assert.Equal(1, result.Counts["duplicated"]);
        Assert.Equal(1, result.Counts["duplicate-deliveries"]);
        Assert.Equal(1, result.Counts["redelivered"]);
    }

    [Fact]
    public void Should_allow_indeterminate_send_to_be_either_received_or_not()
    {
        var ops = new HistoryBuilder()
            .Op("p0", Fn.Send, "p0-0", OpType.Info, error: Errors.Timeout)
            .Op("p0", Fn.Send, "p0-1", OpType.Info, error: Errors.Timeout)
            .Received("p0-0")
            .Build();

        var result = QueueChecker.Check(ops);

        Assert.True(result.Valid);
        Assert.Equal(2, result.Counts["indeterminate"]);
        Assert.Equal(1, result.Counts["indeterminate-received"]);
    }

    [Fact]
    public void Should_not_count_failed_receive_as_received()
    {
        var ops = new HistoryBuilder()
            .Sent("p0-0")
            .Op("c0", Fn.Receive, "p0-0", OpType.Fail, error: "sabotage")
            .Build();

        var result = QueueChecker.Check(ops);

        Assert.Equal("lost", Assert.Single(result.Anomalies).Kind);
    }

    [Fact]
    public void Should_be_unknown_when_nothing_was_sent()
    {
        var result = QueueChecker.Check(new HistoryBuilder().Build());

        Assert.NotNull(result.Unknown);
        Assert.False(result.Valid);
    }
}
