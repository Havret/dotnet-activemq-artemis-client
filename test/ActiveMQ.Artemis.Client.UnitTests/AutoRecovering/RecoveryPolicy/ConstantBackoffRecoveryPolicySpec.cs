using System;
using ActiveMQ.Artemis.Client.AutoRecovering.RecoveryPolicy;
using Xunit;

namespace ActiveMQ.Artemis.Client.UnitTests.AutoRecovering.RecoveryPolicy
{
    public class ConstantBackoffRecoveryPolicySpec
    {
        [Fact]
        public void Should_return_constant_delay()
        {
            var delay = TimeSpan.FromMilliseconds(10);
            var recoveryPolicy = RecoveryPolicyFactory.ConstantBackoff(delay);
            var result = RecoveryPolicyUtils.GetDelays(recoveryPolicy);
            Assert.Equal(new double[] { 10, 10, 10, 10, 10 }, result);
        }

        [Fact]
        public void Should_return_no_limit_retry_count_when_no_retry_count_specified()
        {
            var recoveryPolicy = RecoveryPolicyFactory.ConstantBackoff(TimeSpan.FromSeconds(1));
            Assert.Equal(int.MaxValue, recoveryPolicy.RetryCount);
        }
        
        [Fact]
        public void Should_return_specified_retry_count()
        {
            var retryCount = 10;
            var recoveryPolicy = RecoveryPolicyFactory.ConstantBackoff(TimeSpan.FromSeconds(1), retryCount);
            Assert.Equal(10, recoveryPolicy.RetryCount);
        }

        [Fact]
        public void Should_return_zero_delay_for_first_attempt_when_fast_first_flag_enabled()
        {
            var delay = TimeSpan.FromMilliseconds(10);
            var recoveryPolicy = RecoveryPolicyFactory.ConstantBackoff(delay, fastFirst: true);
            var result = RecoveryPolicyUtils.GetDelays(recoveryPolicy);
            Assert.Equal(new double[] { 0, 10, 10, 10, 10 }, result);
        }
        
        [Fact]
        public void Should_throw_exception_when_initial_delay_less_than_zero()
        {
            var delay = TimeSpan.FromMilliseconds(-1);
            Assert.Throws<ArgumentOutOfRangeException>(() => RecoveryPolicyFactory.ConstantBackoff(delay));
        }
        
        [Fact]
        public void Should_throw_exception_when_retry_count_less_than_zero()
        {
            var delay = TimeSpan.FromMilliseconds(-1);
            var retryCount = -1;
            Assert.Throws<ArgumentOutOfRangeException>(() => RecoveryPolicyFactory.ConstantBackoff(delay, retryCount: retryCount));
        }
    }
}