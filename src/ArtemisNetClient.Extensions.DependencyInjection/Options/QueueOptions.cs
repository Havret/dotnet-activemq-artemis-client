namespace ActiveMQ.Artemis.Client.Extensions.DependencyInjection
{
    /// <summary>
    /// Provides programmatic configuration for a queue to be declared. 
    /// </summary>
    public class QueueOptions
    {
        public int MaxConsumers { get; set; } = -1;

        public bool Exclusive { get; set; }

        public bool GroupRebalance { get; set; }

        public int GroupBuckets { get; set; } = -1;

        public bool PurgeOnNoConsumers { get; set; }

        public bool AutoCreateAddress { get; set; }

        public string FilterExpression { get; set; }
    }
}