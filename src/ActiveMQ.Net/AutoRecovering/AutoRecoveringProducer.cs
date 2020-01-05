using System.Threading.Tasks;

namespace ActiveMQ.Net.AutoRecovering
{
    internal class AutoRecoveringProducer : IProducer
    {
        private readonly string _address;
        private readonly RoutingType _routingType;
        private IProducer _producer;

        public AutoRecoveringProducer(string address, RoutingType routingType)
        {
            _address = address;
            _routingType = routingType;
        }

        public ValueTask DisposeAsync()
        {
            return _producer.DisposeAsync();
        }

        public Task ProduceAsync(Message message)
        {
            return _producer.ProduceAsync(message);
        }

        public void Produce(Message message)
        {
            _producer.Produce(message);
        }

        public void Recover(IConnection connection)
        {
            _producer = connection.CreateProducer(_address, _routingType);
        }
    }
}