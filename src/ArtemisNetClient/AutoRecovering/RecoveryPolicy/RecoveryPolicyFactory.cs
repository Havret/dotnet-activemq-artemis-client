using System;

namespace ActiveMQ.Artemis.Client.AutoRecovering.RecoveryPolicy
{
    public static class RecoveryPolicyFactory
    {
        public static IRecoveryPolicy ConstantBackoff(TimeSpan delay, int retryCount = RecoveryPolicyConsts.NoLimit, bool fastFirst = false)
        {
            return new ConstantBackoffRecoveryPolicy(delay, retryCount, fastFirst);
        }

        public static IRecoveryPolicy LinearBackoff(TimeSpan initialDelay, int retryCount = RecoveryPolicyConsts.NoLimit, double factor = 1.0, bool fastFirst = false)
        {
            return new LinearBackoffRecoveryPolicy(initialDelay, TimeSpan.MaxValue, retryCount, factor, fastFirst);
        }
        
        public static IRecoveryPolicy LinearBackoff(TimeSpan initialDelay, TimeSpan maxDelay, int retryCount = RecoveryPolicyConsts.NoLimit, double factor = 1.0, bool fastFirst = false)
        {
            return new LinearBackoffRecoveryPolicy(initialDelay, maxDelay, retryCount, factor, fastFirst);
        }

        public static IRecoveryPolicy ExponentialBackoff(TimeSpan initialDelay, int retryCount = RecoveryPolicyConsts.NoLimit, double factor = 1.0, bool fastFirst = false)
        {
            return new ExponentialBackoffRecoveryPolicy(initialDelay, TimeSpan.MaxValue, retryCount, factor, fastFirst);
        }
        
        public static IRecoveryPolicy ExponentialBackoff(TimeSpan initialDelay, TimeSpan maxDelay, int retryCount = RecoveryPolicyConsts.NoLimit, double factor = 1.0, bool fastFirst = false)
        {
            return new ExponentialBackoffRecoveryPolicy(initialDelay, maxDelay, retryCount, factor, fastFirst);
        }
        
        public static IRecoveryPolicy DecorrelatedJitterBackoff(TimeSpan medianFirstRetryDelay, int retryCount = RecoveryPolicyConsts.NoLimit, int? seed = null, bool fastFirst = false)
        {
            return new DecorrelatedJitterBackoffRecoveryPolicy(medianFirstRetryDelay, TimeSpan.MaxValue, retryCount, seed, fastFirst);
        }
        
        public static IRecoveryPolicy DecorrelatedJitterBackoff(TimeSpan medianFirstRetryDelay, TimeSpan maxDelay, int retryCount = RecoveryPolicyConsts.NoLimit, int? seed = null, bool fastFirst = false)
        {
            return new DecorrelatedJitterBackoffRecoveryPolicy(medianFirstRetryDelay, maxDelay, retryCount, seed, fastFirst);
        }

        public static IRecoveryPolicy Default()
        {
            return ExponentialBackoff(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(30), factor: 2);
        }
    }
}