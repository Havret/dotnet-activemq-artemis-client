using System;
using Xunit;

namespace ActiveMQ.Artemis.Client.Chaos.UnitTests;

public class LivenessCheckerSpec
{
    private static readonly TimeSpan OperationTimeout = TimeSpan.FromSeconds(10);

    [Fact]
    public void Should_accept_history_where_every_worker_progressed_after_final_heal()
    {
        var ops = new HistoryBuilder()
            .At(10).Nemesis(NemesisValues.FinalHeal)
            .At(20).Sent("p0-0").Received("p0-0")
            .At(40).Nemesis(NemesisValues.Stop)
            .Build();

        var result = LivenessChecker.Check(ops, OperationTimeout);

        Assert.True(result.Valid);
        Assert.Equal(2, result.Counts["workers"]);
    }

    [Fact]
    public void Should_report_worker_without_progress_after_final_heal()
    {
        var ops = new HistoryBuilder()
            .At(1).Sent("p0-0").Received("p0-0")
            .At(10).Nemesis(NemesisValues.FinalHeal)
            .At(20).Sent("p0-1")
            .Op("c0", Fn.Receive, null, OpType.Fail, error: Errors.Timeout)
            .At(40).Nemesis(NemesisValues.Stop)
            .Build();

        var result = LivenessChecker.Check(ops, OperationTimeout);

        Assert.True(result.LivenessViolated);
        var anomaly = Assert.Single(result.Anomalies);
        Assert.Equal("no-progress", anomaly.Kind);
        Assert.Contains("'c0'", anomaly.Description);
    }

    [Fact]
    public void Should_report_operation_that_never_completed_as_hung()
    {
        var ops = new HistoryBuilder()
            .At(5).Add("p0", OpType.Invoke, Fn.Send, "p0-0")
            .At(10).Nemesis(NemesisValues.FinalHeal)
            .At(20).Received("x")
            .At(40).Nemesis(NemesisValues.Stop)
            .Build();

        var result = LivenessChecker.Check(ops, OperationTimeout);

        Assert.Contains(result.Anomalies, x => x.Kind == "hung" && x.Description.Contains("'p0'"));
    }

    [Fact]
    public void Should_report_operation_that_outlived_its_timeout()
    {
        var ops = new HistoryBuilder()
            .At(10).Nemesis(NemesisValues.FinalHeal)
            .At(11).Add("p0", OpType.Invoke, Fn.Send, "p0-0")
            .At(30).Add("p0", OpType.Ok, Fn.Send, "p0-0")
            .Received("p0-0")
            .At(40).Nemesis(NemesisValues.Stop)
            .Build();

        var result = LivenessChecker.Check(ops, OperationTimeout);

        Assert.Equal("ignored-cancellation", Assert.Single(result.Anomalies).Kind);
    }

    [Fact]
    public void Should_be_unknown_when_run_did_not_reach_final_heal()
    {
        var ops = new HistoryBuilder().Sent("p0-0").Build();

        var result = LivenessChecker.Check(ops, OperationTimeout);

        Assert.NotNull(result.Unknown);
    }
}
