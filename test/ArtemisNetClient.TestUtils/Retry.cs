using System;
using System.Threading;
using System.Threading.Tasks;

namespace ActiveMQ.Artemis.Client.TestUtils
{
    public static class Retry
    {
        public static async Task<T> RetryUntil<T>(Func<Task<T>> func, Func<T, bool> until, TimeSpan timeout)
        {
            var cts = new CancellationTokenSource(timeout);
            while (true)
            {
                var result = await func();
                if (until(result))
                    return result;
                if (cts.IsCancellationRequested)
                    return result;
                
                // ReSharper disable once MethodSupportsCancellation
                await Task.Delay(TimeSpan.FromMilliseconds(100));
            }
        }
    }
}