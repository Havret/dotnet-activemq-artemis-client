namespace ActiveMQ.Net
{
    public interface IBaseProducerConfiguration
    {
        byte? MessagePriority { get; }
        DurabilityMode? MessageDurabilityMode { get; }
    }
}