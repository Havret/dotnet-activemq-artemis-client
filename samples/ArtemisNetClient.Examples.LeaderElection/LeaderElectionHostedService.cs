using System;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Extensions.LeaderElection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ActiveMQ.Artemis.Client.Examples.LeaderElection
{
    public class LeaderElectionHostedService : IHostedService
    {
        private readonly ILogger<LeaderElectionHostedService> _logger;
        private readonly ILeaderElection _leaderElection;
        private CancellationTokenSource _cts;
        private Task _task;

        public LeaderElectionHostedService(ILogger<LeaderElectionHostedService> logger, ILeaderElection leaderElection)
        {
            _logger = logger;
            _leaderElection = leaderElection;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _cts = new CancellationTokenSource();
            _task = Task.Run(async () =>
            {
                var token = _cts.Token;
                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        await _leaderElection.TakeLeadershipAsync(token);
                        while (_leaderElection.IsLeader() && !token.IsCancellationRequested)
                        {
                            await DoSomeWork();
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Ignore
                }
            });

            return Task.CompletedTask;
        }

        private async Task DoSomeWork()
        {
            _logger.LogInformation("I am the leader!");
            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _cts.Cancel();
            _cts.Dispose();
            return _task;
        }
    }
}