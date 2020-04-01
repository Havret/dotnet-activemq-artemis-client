using System.Linq;
using ActiveMQ.Net.AutoRecovering.RecoveryPolicy;

namespace ActiveMQ.Net.Tests.AutoRecovering.RecoveryPolicy
{
    public static class RecoveryPolicyUtils
    {
        public static double[] GetDelays(IRecoveryPolicy recoveryPolicy)
        {
            return Enumerable.Range(1, 5).Select(x => recoveryPolicy.GetDelay(x).TotalMilliseconds).ToArray();
        }
    }
}