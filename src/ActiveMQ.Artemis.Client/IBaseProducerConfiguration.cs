namespace ActiveMQ.Artemis.Client
{
    public interface IBaseProducerConfiguration
    {
        byte? MessagePriority { get; }
        DurabilityMode? MessageDurabilityMode { get; }
        public bool SetMessageCreationTime { get; }
    }
}