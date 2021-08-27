using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;

namespace ActiveMQ.Artemis.Client.Extensions.LeaderElection
{
    internal class LeaderElection : ILeaderElection, IAsyncDisposable
    {
        private readonly ILogger<LeaderElection> _logger;
        private readonly ElectionMessageProducer _producer;
        private readonly LeaderElectionOptions _options;
        private readonly AsyncManualResetEvent _manualResetEvent = new AsyncManualResetEvent();
        private readonly CancellationTokenSource _cts;
        private readonly object _mutex = new object();
        
        private bool _disposed;
        private Task _task;
        private volatile int _electionMsgCounter;

        public LeaderElection(ILogger<LeaderElection> logger, ElectionMessageProducer producer, LeaderElectionOptions options)
        {
            _logger = logger;
            _producer = producer;
            _options = options;
            _cts = new CancellationTokenSource();
        }

        public Task TakeLeadershipAsync(CancellationToken cancellationToken)
        {
            StartElectionLoop();
            return _manualResetEvent.WaitAsync(cancellationToken);
        }

        private void StartElectionLoop()
        {
            if (_task != null)
            {
                return;
            }

            lock (_mutex)
            {
                _task ??= Task.Run(ElectionLoop);
            }
        }

        private async Task ElectionLoop()
        {
            var token = _cts.Token;
            while (!token.IsCancellationRequested)
            {
                try
                {
                    _producer.SendElectionMessage(token);
                    Interlocked.Increment(ref _electionMsgCounter);

                    await Task.Delay(_options.ElectionMessageInterval, token).ConfigureAwait(false);

                    if (_electionMsgCounter > _options.HandOverAfterMissedElectionMessages)
                    {
                        Interlocked.Exchange(ref _electionMsgCounter, 0);

                        var wasLeader = IsLeader();
                        HandOverLeadership();

                        if (wasLeader && !IsLeader())
                        {
                            _logger.LogInformation($"Leadership handed over as number of missed election messages exceeded {_options.HandOverAfterMissedElectionMessages}");
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Exception occurred in send election message loop.");
                }
            }
        }


        public bool IsLeader()
        {
            return _manualResetEvent.IsSet;
        }

        public void OnElectionMessage()
        {
            var wasLeader = IsLeader();
            TakeOverLeadership();
            if (!wasLeader && IsLeader())
            {
                _logger.LogInformation("Leadership taken over.");
            }

            Interlocked.Exchange(ref _electionMsgCounter, 0);
        }

        private void HandOverLeadership()
        {
            _manualResetEvent.Reset();
        }

        private void TakeOverLeadership()
        {
            _manualResetEvent.Set();
        }

        public async ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                _cts.Cancel();
                await _task.ConfigureAwait(false);
                _cts.Dispose();
                _disposed = true;
            }
        }
    }
}