using System.Threading;
using System.Threading.Tasks;

namespace ActiveMQ.Artemis.Client.AutoRecovering
{
    internal delegate void Closed(IRecoverable recoverable);

    internal delegate void RecoveryRequested();

    internal interface IRecoverable
    {
        Task RecoverAsync(IConnection connection, CancellationToken cancellationToken);
        void Suspend();
        void Resume();
        event Closed Closed;
        event RecoveryRequested RecoveryRequested;
    }
}