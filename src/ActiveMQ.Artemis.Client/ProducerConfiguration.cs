namespace ActiveMQ.Artemis.Client
{
    public class ProducerConfiguration : IBaseProducerConfiguration
    {
        public string Address { get; set; }
        public AddressRoutingType RoutingType { get; set; }
        public byte? MessagePriority { get; set; }
        public DurabilityMode? MessageDurabilityMode { get; set; }
        public bool SetMessageCreationTime { get; set; } = true;
    }
}