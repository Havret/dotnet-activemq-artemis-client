using ActiveMQ.Artemis.Client.MessageIdPolicy;

namespace ActiveMQ.Artemis.Client
{
    public class ProducerConfiguration : IBaseProducerConfiguration
    {
        public string Address { get; set; }
        public RoutingType? RoutingType { get; set; }
        public byte? MessagePriority { get; set; }
        public DurabilityMode? MessageDurabilityMode { get; set; }
        public bool SetMessageCreationTime { get; set; } = true;
        public IMessageIdPolicy MessageIdPolicy { get; set; }
    }
}