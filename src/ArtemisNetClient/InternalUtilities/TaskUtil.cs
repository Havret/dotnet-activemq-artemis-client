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
                    try
                    {
                        cleanup?.Invoke();
                    }
                    catch (Exception)
                    {
                        // Cleanup is best effort. This callback may run on a timer thread (e.g. CancelAfter),
                        // where an unhandled exception would crash the process. E.g. SenderLink.Cancel throws
                        // when the message has already left the outgoing queue.
                    }
                }
            }) : default;
            return (tcs, ctr);
        }
    }
}