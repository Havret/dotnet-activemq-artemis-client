using System.Threading;
using System.Threading.Tasks;

namespace ActiveMQ.Artemis.Client.InternalUtilities
{
    internal static class TaskUtil
    {
        public static TaskCompletionSource<T> CreateTaskCompletionSource<T>(CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            if (cancellationToken != default)
            {
                cancellationToken.Register(() => tcs.TrySetCanceled());
            }

            return tcs;
        }
    }
}