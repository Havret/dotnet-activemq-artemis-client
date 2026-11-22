using Xunit;

namespace ActiveMQ.Artemis.Client.Chaos.UnitTests;

public class RequestReplyCheckerSpec
{
    [Fact]
    public void Should_accept_history_where_every_request_got_its_own_reply()
    {
        var ops = new HistoryBuilder()
            .Add("r0", OpType.Invoke, Fn.Request, "r0-0")
            .Op("responder", Fn.Respond, "r0-0", OpType.Ok)
            .Add("r0", OpType.Ok, Fn.Request, "r0-0", result: "r0-0")
            .Build();

        var result = RequestReplyChecker.Check(ops);

        Assert.True(result.Valid);
        Assert.Equal(1, result.Counts["ok"]);
    }

    [Fact]
    public void Should_report_request_completed_with_reply_to_another_request()
    {
        var ops = new HistoryBuilder()
            .Add("r0", OpType.Invoke, Fn.Request, "r0-0")
            .Add("r1", OpType.Invoke, Fn.Request, "r1-0")
            .Op("responder", Fn.Respond, "r0-0", OpType.Ok)
            .Op("responder", Fn.Respond, "r1-0", OpType.Ok)
            .Add("r0", OpType.Ok, Fn.Request, "r0-0", result: "r1-0")
            .Add("r1", OpType.Ok, Fn.Request, "r1-0", result: "r1-0")
            .Build();

        var result = RequestReplyChecker.Check(ops);

        Assert.True(result.SafetyViolated);
        var anomaly = Assert.Single(result.Anomalies);
        Assert.Equal("crossed-reply", anomaly.Kind);
        Assert.Contains("'r0-0'", anomaly.Description);
    }

    [Fact]
    public void Should_report_reply_to_request_that_responder_never_received()
    {
        var ops = new HistoryBuilder()
            .Op("r0", Fn.Request, "r0-0", OpType.Ok, result: "r0-0")
            .Build();

        var result = RequestReplyChecker.Check(ops);

        Assert.Equal("phantom-reply", Assert.Single(result.Anomalies).Kind);
    }

    [Fact]
    public void Should_allow_requests_failed_with_client_closed()
    {
        var ops = new HistoryBuilder()
            .Op("r0", Fn.Request, "r0-0", OpType.Info, error: "RequestReplyClientClosedException: The Request Reply Client was closed.")
            .Build();

        var result = RequestReplyChecker.Check(ops);

        Assert.True(result.Valid);
        Assert.Equal(1, result.Counts["client-closed"]);
    }
}
