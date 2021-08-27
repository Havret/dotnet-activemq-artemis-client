using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ActiveMQ.Artemis.Client.Extensions.Hosting
{
    internal class ActiveMqHostedService : IHostedService
    {
        private readonly IActiveMqClient _activeMqClient;

        public ActiveMqHostedService(IActiveMqClient activeMqClient)
        {
            _activeMqClient = activeMqClient;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return _activeMqClient.StartAsync(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return _activeMqClient.StopAsync(cancellationToken);
        }
    }
}