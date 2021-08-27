using System.Threading;
using System.Threading.Tasks;

namespace ActiveMQ.Artemis.Client.Extensions.LeaderElection
{
    public interface ILeaderElection
    {
        Task TakeLeadershipAsync(CancellationToken cancellationToken);
        bool IsLeader();
    }
}