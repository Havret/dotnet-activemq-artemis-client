using System;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ActiveMQ.Artemis.Client.Extensions.Hosting
{
    internal class ActiveMqBackgroundService : BackgroundService
    {
        private readonly IActiveMqClient _activeMqClient;
        private readonly ILogger<ActiveMqBackgroundService> _logger;

        public ActiveMqBackgroundService(IActiveMqClient activeMqClient, ILogger<ActiveMqBackgroundService> logger)
        {
            _activeMqClient = activeMqClient;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Don't hold up the host startup until the first await.
            await Task.Yield();

            try
            {
                await _activeMqClient.StartAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Exception occured during ActiveMQ client startup.");
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await base.StopAsync(cancellationToken).ConfigureAwait(false);
            await _activeMqClient.StopAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}