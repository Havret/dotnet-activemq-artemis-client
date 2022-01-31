using System.Threading;
using System.Threading.Tasks;

namespace ActiveMQ.Artemis.Client.Extensions.DependencyInjection
{
    internal interface IActiveMqRequestReplyClient
    {
        ValueTask StartAsync(CancellationToken cancellationToken);
        ValueTask StopAsync();
    }
}