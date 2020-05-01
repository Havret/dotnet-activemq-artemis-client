namespace ActiveMQ.Net
{
    public class AnonymousProducerConfiguration : IBaseProducerConfiguration
    {
        public byte? MessagePriority { get; set; }
        public DurabilityMode? MessageDurabilityMode { get; set; }
    }
}