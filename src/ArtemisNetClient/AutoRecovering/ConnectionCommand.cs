using System.Threading;
using System.Threading.Tasks;

namespace ActiveMQ.Artemis.Client.AutoRecovering
{
    /// <summary>
    /// A message processed by the connection's command loop. The loop is the only place that touches the
    /// underlying connection and the set of recoverables, which keeps recovery and creation from interleaving.
    /// </summary>
    internal abstract class ConnectionCommand
    {
    }

    /// <summary>
    /// Asks the loop to make sure the connection is established and the recoverables are resumed.
    /// </summary>
    internal sealed class ConnectCommand : ConnectionCommand
    {
        private ConnectCommand() { }
        public static readonly ConnectCommand Instance = new();
    }

    /// <summary>
    /// Asks the loop to create a recoverable on the current connection and start tracking it.
    /// </summary>
    internal sealed class CreateRecoverableCommand : ConnectionCommand
    {
        public CreateRecoverableCommand(IRecoverable recoverable, CancellationToken cancellationToken)
        {
            Recoverable = recoverable;
            CancellationToken = cancellationToken;
            Completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public IRecoverable Recoverable { get; }
        public CancellationToken CancellationToken { get; }
        public TaskCompletionSource<bool> Completion { get; }
    }
}