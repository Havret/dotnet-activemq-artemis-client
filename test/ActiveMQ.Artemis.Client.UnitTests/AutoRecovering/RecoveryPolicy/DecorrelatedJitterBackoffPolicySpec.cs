using System;
using System.Linq;
using ActiveMQ.Artemis.Client.AutoRecovering.RecoveryPolicy;
using Xunit;

namespace ActiveMQ.Artemis.Client.UnitTests.AutoRecovering.RecoveryPolicy
{
    public class DecorrelatedJitterBackoffPolicySpec
    {
        [Fact]
        public void Should_return_specified_retry_count()
        {
            var retryCount = 10;
            var recoveryPolicy = RecoveryPolicyFactory.DecorrelatedJitterBackoff(TimeSpan.FromSeconds(1), retryCount);
            Assert.Equal(10, recoveryPolicy.RetryCount);
        }
        
        [Fact]
        public void Should_return_no_limit_retry_count_when_no_retry_count_specified()
        {
            var recoveryPolicy = RecoveryPolicyFactory.DecorrelatedJitterBackoff(TimeSpan.FromSeconds(1));
            Assert.Equal(int.MaxValue, recoveryPolicy.RetryCount);
        }

        [Fact]
        public void Should_return_zero_when_fast_first_equal_to_true()
        {
            // Arrange
            var medianFirstDelay = TimeSpan.FromSeconds(2);
            const int retryCount = 10;
            const bool fastFirst = true;
            const int seed = 1;

            // Act
            var recoveryPolicy = RecoveryPolicyFactory.DecorrelatedJitterBackoff(medianFirstRetryDelay: medianFirstDelay, retryCount: retryCount, seed: seed, fastFirst: fastFirst);
            var timeSpans = Enumerable.Range(1, retryCount).Select(x => recoveryPolicy.GetDelay(x)).ToList();

            // Assert
            bool first = true;
            int t = 0;
            foreach (var timeSpan in timeSpans)
            {
                if (first)
                {
                    Assert.Equal(TimeSpan.Zero, timeSpan);
                    first = false;
                }
                else
                {
                    t++;
                    AssertOnRetryDelayForTry(t, timeSpan, medianFirstDelay);
                }
            }
        }
        
        
        [Fact]
        public void Should_return_delay_in_range()
        {
            // Arrange
            var medianFirstDelay = TimeSpan.FromSeconds(1);
            const int retryCount = 6;
            const bool fastFirst = false;
            const int seed = 23456;

            // Act
            var recoveryPolicy = RecoveryPolicyFactory.DecorrelatedJitterBackoff(medianFirstRetryDelay: medianFirstDelay, retryCount: retryCount, seed: seed, fastFirst: fastFirst);
            var timeSpans = Enumerable.Range(1, retryCount).Select(x => recoveryPolicy.GetDelay(x)).ToList();

            // Assert
            int t = 0;
            foreach (var timeSpan in timeSpans)
            {
                t++;
                AssertOnRetryDelayForTry(t, timeSpan, medianFirstDelay);
            }
        }
        
        [Fact]
        public void Should_return_delay_in_range_wide()
        {
            // Arrange
            var medianFirstDelay = TimeSpan.FromSeconds(3);
            const int retryCount = 6;
            const bool fastFirst = false;
            const int seed = 23456;

            // Act
            var recoveryPolicy = RecoveryPolicyFactory.DecorrelatedJitterBackoff(medianFirstRetryDelay: medianFirstDelay, retryCount: retryCount, seed: seed, fastFirst: fastFirst);
            var timeSpans = Enumerable.Range(1, retryCount).Select(x => recoveryPolicy.GetDelay(x)).ToList();

            // Assert
            int t = 0;
            foreach (var timeSpan in timeSpans)
            {
                t++;
                AssertOnRetryDelayForTry(t, timeSpan, medianFirstDelay);
            }
        }
        
        [Fact]
        public void Should_throw_exception_when_median_first_retry_delay_less_than_zero()
        {
            var delay = TimeSpan.FromMilliseconds(-1);
            Assert.Throws<ArgumentOutOfRangeException>(() => RecoveryPolicyFactory.DecorrelatedJitterBackoff(delay));
        }
        
        [Fact]
        public void Should_throw_exception_when_retry_count_less_than_zero()
        {
            var delay = TimeSpan.FromMilliseconds(1);
            var retryCount = -1;
            Assert.Throws<ArgumentOutOfRangeException>(() => RecoveryPolicyFactory.DecorrelatedJitterBackoff(delay, retryCount: retryCount));
        }

        private static void AssertOnRetryDelayForTry(int t, TimeSpan calculatedDelay, TimeSpan medianFirstDelay)
        {
            Assert.True(calculatedDelay >= TimeSpan.Zero);
            int upperLimitFactor = t < 2 ? (int)Math.Pow(2, t + 1) : (int)(Math.Pow(2, t + 1) - Math.Pow(2, t - 1));
            Assert.True(calculatedDelay <= TimeSpan.FromTicks(medianFirstDelay.Ticks * upperLimitFactor));
        }
    }
}