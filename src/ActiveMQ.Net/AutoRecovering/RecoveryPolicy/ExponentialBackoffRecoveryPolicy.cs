using System;

namespace ActiveMQ.Net.AutoRecovering.RecoveryPolicy
{
    internal class ExponentialBackoffRecoveryPolicy : IRecoveryPolicy
    {
        private readonly TimeSpan _initialDelay;
        private readonly TimeSpan _maxDelay;
        private readonly double _factor;
        private readonly bool _fastFirst;

        public ExponentialBackoffRecoveryPolicy(TimeSpan initialDelay, TimeSpan maxDelay, int retryCount, double factor = 2.0, bool fastFirst = false)
        {
            if (initialDelay < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(initialDelay), initialDelay, "should be >= 0ms");
            if (maxDelay < initialDelay) throw new ArgumentOutOfRangeException(nameof(maxDelay), maxDelay, "should be >= initialDelay");
            if (retryCount < 0) throw new ArgumentOutOfRangeException(nameof(retryCount), retryCount, "should be >= 0");
            if (factor < 1.0) throw new ArgumentOutOfRangeException(nameof(factor), factor, "should be >= 1.0");

            _initialDelay = initialDelay;
            _maxDelay = maxDelay;
            _factor = factor;
            _fastFirst = fastFirst;
            RetryCount = retryCount;
        }

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
            
            var delayMilliseconds = _initialDelay.TotalMilliseconds * Math.Pow(_factor, attempt - 1);
            return TimeSpan.FromMilliseconds(Math.Min(delayMilliseconds, _maxDelay.TotalMilliseconds));
        }

        public int RetryCount { get; }
    }
}