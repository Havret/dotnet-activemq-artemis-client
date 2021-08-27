using System.Linq;
using ActiveMQ.Artemis.Client.AutoRecovering.RecoveryPolicy;

namespace ActiveMQ.Artemis.Client.UnitTests.AutoRecovering.RecoveryPolicy
{
    public static class RecoveryPolicyUtils
    {
        public static double[] GetDelays(IRecoveryPolicy recoveryPolicy)
        {
            return Enumerable.Range(1, 5).Select(x => recoveryPolicy.GetDelay(x).TotalMilliseconds).ToArray();
        }
    }
}