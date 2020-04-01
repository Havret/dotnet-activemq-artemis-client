using System;

namespace ActiveMQ.Net.AutoRecovering.RecoveryPolicy
{
    public interface IRecoveryPolicy
    {
        int RetryCount { get; }
        TimeSpan GetDelay(int attempt);
    }
}