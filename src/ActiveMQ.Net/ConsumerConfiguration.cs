namespace ActiveMQ.Net
{
    public class ConsumerConfiguration
    {
        public string Address { get; set; }
        public QueueRoutingType RoutingType { get; set; }
        public string Queue { get; set; }
        public int Credit { get; set; } = 200;
        public string FilterExpression { get; set; }
    }
}