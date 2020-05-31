namespace ActiveMQ.Artemis.Client
{
    public class QueueConfiguration
    {
        public string Name { get; set; }
        public RoutingType RoutingType { get; set; }
        public string Address { get; set; }
        public bool Durable { get; set; } = true;
        public int MaxConsumers { get; set; } = -1;
        public bool Exclusive { get; set; }
        public bool GroupRebalance { get; set; }
        public int GroupBuckets { get; set; } = -1;
        public bool PurgeOnNoConsumers { get; set; }
        public bool AutoCreateAddress { get; set; }
    }
}