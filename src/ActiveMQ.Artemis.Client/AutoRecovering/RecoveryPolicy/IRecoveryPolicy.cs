using System;

namespace ActiveMQ.Artemis.Client.AutoRecovering.RecoveryPolicy
{
    public interface IRecoveryPolicy
    {
        int RetryCount { get; }
        TimeSpan GetDelay(int attempt);
    }
}