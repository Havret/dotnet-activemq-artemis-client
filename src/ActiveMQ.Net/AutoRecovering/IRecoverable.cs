using System.Threading.Tasks;

namespace ActiveMQ.Net.AutoRecovering
{
    internal delegate void Closed(IRecoverable recoverable);

    internal delegate void RecoveryRequested();

    internal interface IRecoverable
    {
        Task RecoverAsync(IConnection connection);
        void Suspend();
        void Resume();
        event Closed Closed;
        event RecoveryRequested RecoveryRequested;
    }
}