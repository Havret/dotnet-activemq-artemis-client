using System;
using System.Threading;
using System.Threading.Tasks;

namespace ActiveMQ.Artemis.Client.InternalUtilities
{
    internal static class TaskUtil
    {
        public static (TaskCompletionSource<T> tcs, CancellationTokenRegistration ctr) CreateTaskCompletionSource<T>(ref CancellationToken cancellationToken)
        {
            return CreateTaskCompletionSource<T>(ref cancellationToken, null);
        }
        
        public static (TaskCompletionSource<T> tcs, CancellationTokenRegistration ctr) CreateTaskCompletionSource<T>(ref CancellationToken cancellationToken, Action cleanup)
        {
            var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            var ctr = cancellationToken != default ? cancellationToken.Register(() =>
            {
                if (tcs.TrySetCanceled())
                {
                    cleanup?.Invoke();
                }
            }) : default;
            return (tcs, ctr);
        }
    }
}