using System;

namespace ActiveMQ.Artemis.Client.AutoRecovering.RecoveryPolicy
{
    internal class LinearBackoffRecoveryPolicy : IRecoveryPolicy
    {
        private readonly TimeSpan _initialDelay;
        private readonly TimeSpan _maxDelay;
        private readonly double _factor;
        private readonly bool _fastFirst;

        public LinearBackoffRecoveryPolicy(TimeSpan initialDelay, TimeSpan maxDelay, int retryCount, double factor, bool fastFirst)
        {
            if (initialDelay < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(initialDelay), initialDelay, "should be >= 0ms");
            if (maxDelay < initialDelay) throw new ArgumentOutOfRangeException(nameof(maxDelay), maxDelay, "should be >= initialDelay");
            if (retryCount < 0) throw new ArgumentOutOfRangeException(nameof(retryCount), retryCount, "should be >= 0");
            if (factor < 0) throw new ArgumentOutOfRangeException(nameof(factor), factor, "should be >= 0");

            _initialDelay = initialDelay;
            _maxDelay = maxDelay;
            _factor = factor;
            _fastFirst = fastFirst;
            RetryCount = retryCount;
        }

        public int RetryCount { get; }

        public TimeSpan GetDelay(int attempt)
        {
            if (_fastFirst)
            {
                if (attempt == 1)
                {
                    return TimeSpan.Zero;
                }

                attempt--;
            }

            var delayMilliseconds = _initialDelay.TotalMilliseconds * (1 + (attempt - 1) * _factor);
            return TimeSpan.FromMilliseconds(Math.Min(delayMilliseconds, _maxDelay.TotalMilliseconds));
        }
    }
}