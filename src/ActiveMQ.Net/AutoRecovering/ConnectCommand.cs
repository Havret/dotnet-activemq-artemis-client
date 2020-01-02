using System.Threading.Tasks;

namespace ActiveMQ.Net.AutoRecovering
{
    internal class ConnectCommand
    {
        private TaskCompletionSource<bool> _taskCompletionSource;

        public static readonly ConnectCommand Empty = new ConnectCommand();

        public static ConnectCommand InitialConnect(TaskCompletionSource<bool> taskCompletionSource) =>
            new ConnectCommand { _taskCompletionSource = taskCompletionSource };

        public void NotifyWaiter()
        {
            _taskCompletionSource?.SetResult(true);
        }
    }
}