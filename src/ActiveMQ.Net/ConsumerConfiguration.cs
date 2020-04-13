namespace ActiveMQ.Net
{
    public class ConsumerConfiguration
    {
        public string Address { get; set; }
        public RoutingType RoutingType { get; set; }
        public string Queue { get; set; }
    }
}