namespace ActiveMQ.Artemis.Client
{
    public class ConsumerConfiguration
    {
        public string Address { get; set; }
        public RoutingType? RoutingType { get; set; }
        public string Queue { get; set; }
        public int Credit { get; set; } = 200;
        public string FilterExpression { get; set; }
        public bool NoLocalFilter { get; set; }
        public bool Shared { get; set; }
    }
}