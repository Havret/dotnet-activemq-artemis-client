namespace ActiveMQ.Artemis.Client.Chaos;

public static class Worker
{
    /// <summary>
    /// Runs a worker loop on the thread pool. An unexpected crash of the loop is recorded instead of being lost,
    /// the liveness checker then reports the worker as not making progress.
    /// </summary>
    public static Task Run(History history, string process, Func<Task> loop)
    {
        return Task.Run(async () =>
        {
            try
            {
                await loop();
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception e)
            {
                history.Record(process, Fn.Lifecycle, OpType.Info, "worker crashed", RunContext.Describe(e));
            }
        });
    }

    public static async Task Pause(TimeSpan delay, CancellationToken ct)
    {
        try
        {
            await Task.Delay(delay, ct);
        }
        catch (OperationCanceledException)
        {
        }
    }

    public static async Task DisposeQuietly(IAsyncDisposable disposable)
    {
        if (disposable == null)
        {
            return;
        }

        try
        {
            await disposable.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10));
        }
        catch (Exception)
        {
            // shutting down, nothing to do about it
        }
    }
}
