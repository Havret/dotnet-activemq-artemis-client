using System;

namespace ActiveMQ.Artemis.Client.Extensions.LeaderElection
{
    public class LeaderElectionOptions
    {
        public string ElectionAddress { get; set; } = "LeaderElectionAddress";
        public TimeSpan ElectionMessageInterval { get; set; } = TimeSpan.FromSeconds(1);
        public int HandOverAfterMissedElectionMessages { get; set; } = 3;
    }
}