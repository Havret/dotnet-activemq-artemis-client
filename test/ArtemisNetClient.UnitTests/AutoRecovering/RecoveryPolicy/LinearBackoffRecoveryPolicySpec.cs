using System;
using ActiveMQ.Artemis.Client.AutoRecovering.RecoveryPolicy;
using Xunit;

namespace ActiveMQ.Artemis.Client.UnitTests.AutoRecovering.RecoveryPolicy
{
    public class LinearBackoffRecoveryPolicySpec
    {
        [Theory]
        [InlineData(10, 2, new double[] { 10, 30, 50, 70, 90 })]
        [InlineData(10, 3, new double[] { 10, 40, 70, 100, 130 })]
        [InlineData(10, 4, new double[] { 10, 50, 90, 130, 170 })]
        public void Should_return_delays_in_a_linear_manner(double initialDelayMilliseconds, double factor, double[] delays)
        {
            var initialDelay = TimeSpan.FromMilliseconds(initialDelayMilliseconds);
            var recoveryPolicy = RecoveryPolicyFactory.LinearBackoff(initialDelay, factor: factor);
            var result = RecoveryPolicyUtils.GetDelays(recoveryPolicy);
            Assert.Equal(delays, result);
        }

        [Fact]
        public void Should_not_return_delay_longer_than_specified_max_delay()
        {
            var initialDelay = TimeSpan.FromMilliseconds(10);
            var maxDelay = TimeSpan.FromMilliseconds(90);
            var factor = 3;
            var recoveryPolicy = RecoveryPolicyFactory.LinearBackoff(initialDelay, maxDelay, factor: factor);
            var result = RecoveryPolicyUtils.GetDelays(recoveryPolicy);
            Assert.Equal(new double[] { 10, 40, 70, 90, 90 }, result);
        }

        [Fact]
        public void Should_return_specified_retry_count()
        {
            var retryCount = 10;
            var recoveryPolicy = RecoveryPolicyFactory.LinearBackoff(TimeSpan.FromSeconds(1), retryCount);
            Assert.Equal(retryCount, recoveryPolicy.RetryCount);
        }

        [Fact]
        public void Should_return_no_limit_retry_count_when_no_retry_count_specified()
        {
            var recoveryPolicy = RecoveryPolicyFactory.LinearBackoff(TimeSpan.FromSeconds(1));
            Assert.Equal(int.MaxValue, recoveryPolicy.RetryCount);
        }

        [Fact]
        public void Should_return_constant_delay_when_factor_is_zero()
        {
            var initialDelay = TimeSpan.FromMilliseconds(10);
            var factor = 0;
            var recoveryPolicy = RecoveryPolicyFactory.LinearBackoff(initialDelay, factor: factor);
            var result = RecoveryPolicyUtils.GetDelays(recoveryPolicy);
            Assert.Equal(new double[] { 10, 10, 10, 10, 10 }, result);
        }

        [Fact]
        public void Should_return_zero_delay_for_the_first_attempt_when_fast_first_flag_enabled()
        {
            var initialDelay = TimeSpan.FromMilliseconds(10);
            var factor = 2;
            var recoveryPolicy = RecoveryPolicyFactory.LinearBackoff(initialDelay, factor: factor, fastFirst: true);
            var result = RecoveryPolicyUtils.GetDelays(recoveryPolicy);
            Assert.Equal(new double[] { 0, 10, 30, 50, 70 }, result);
        }

        [Fact]
        public void Should_return_constant_delay_when_factor_is_zero_and_fast_first_flag_enabled()
        {
            var initialDelay = TimeSpan.FromMilliseconds(10);
            var factor = 0;
            var recoveryPolicy = RecoveryPolicyFactory.LinearBackoff(initialDelay, factor: factor, fastFirst: true);
            var result = RecoveryPolicyUtils.GetDelays(recoveryPolicy);
            Assert.Equal(new double[] { 0, 10, 10, 10, 10 }, result);
        }

        [Fact]
        public void Should_throw_exception_when_initial_delay_less_than_zero()
        {
            var initialDelay = TimeSpan.FromMilliseconds(-1);
            Assert.Throws<ArgumentOutOfRangeException>(() => RecoveryPolicyFactory.LinearBackoff(initialDelay));
        }

        [Fact]
        public void Should_throw_exception_when_retry_count_less_than_zero()
        {
            var initialDelay = TimeSpan.FromMilliseconds(10);
            var retryCount = -1;
            Assert.Throws<ArgumentOutOfRangeException>(() => RecoveryPolicyFactory.LinearBackoff(initialDelay, retryCount: retryCount));
        }

        [Fact]
        public void Should_throw_exception_when_factor_less_than_zero()
        {
            var initialDelay = TimeSpan.FromMilliseconds(10);
            var factor = -1;
            Assert.Throws<ArgumentOutOfRangeException>(() => RecoveryPolicyFactory.LinearBackoff(initialDelay, factor: factor));
        }

        [Fact]
        public void Should_throw_exception_when_max_delay_less_than_initial_delay()
        {
            var initialDelay = TimeSpan.FromMilliseconds(10);
            var maxDelay = TimeSpan.FromMilliseconds(9);
            Assert.Throws<ArgumentOutOfRangeException>(() => RecoveryPolicyFactory.LinearBackoff(initialDelay, maxDelay));
        }
    }
}