using System.Threading.Tasks;

namespace ActiveMQ.Net.AutoRecovering
{
    internal delegate void Closed(IRecoverable recoverable);

    internal interface IRecoverable
    {
        Task RecoverAsync(IConnection connection);
        event Closed Closed;
    }
}