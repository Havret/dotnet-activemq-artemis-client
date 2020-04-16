namespace ActiveMQ.Net
{
    public class ConsumerConfiguration
    {
        public string Address { get; set; }
        public QueueRoutingType RoutingType { get; set; }
        public string Queue { get; set; }
    }
}