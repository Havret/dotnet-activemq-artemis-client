using System;

namespace ActiveMQ.Net.AutoRecovering.RecoveryPolicy
{
    internal class ConstantBackoffRecoveryPolicy : IRecoveryPolicy
    {
        private readonly TimeSpan _delay;
        private readonly bool _fastFirst;

        public ConstantBackoffRecoveryPolicy(TimeSpan delay, int retryCount = RecoveryPolicyConsts.NoLimit, bool fastFirst = false)
        {
            if (delay < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(delay), delay, "should be >= 0ms");
            if (retryCount < 0) throw new ArgumentOutOfRangeException(nameof(retryCount), retryCount, "should be >= 0");
            
            _delay = delay;
            _fastFirst = fastFirst;
            RetryCount = retryCount;
        }

        public int RetryCount { get; }

        public TimeSpan GetDelay(int attempt)
        {
            if (_fastFirst && attempt == 1)
            {
                return TimeSpan.Zero;
            }

            return _delay;
        }
    }
}