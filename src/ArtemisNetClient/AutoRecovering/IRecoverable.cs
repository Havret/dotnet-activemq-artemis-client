using System;
using System.Threading;
using System.Threading.Tasks;

namespace ActiveMQ.Artemis.Client.AutoRecovering
{
    internal delegate void Closed(IRecoverable recoverable);

    internal delegate void RecoveryRequested();

    internal interface IRecoverable : IAsyncDisposable
    {
        Task RecoverAsync(IConnection connection, CancellationToken cancellationToken);
        void Suspend();
        void Resume();

        /// <summary>
        /// True when the recoverable has been suspended, e.g. because its underlying link was closed, and is waiting to be recovered.
        /// </summary>
        bool IsSuspended { get; }
        event Closed Closed;
        event RecoveryRequested RecoveryRequested;
        Task TerminateAsync(Exception exception);
    }
}