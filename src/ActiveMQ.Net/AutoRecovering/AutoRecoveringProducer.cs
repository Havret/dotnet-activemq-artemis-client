using System.Threading.Tasks;

namespace ActiveMQ.Net.AutoRecovering
{
    internal class AutoRecoveringProducer : IProducer, IRecoverable
    {
        private readonly string _address;
        private readonly RoutingType _routingType;
        private IProducer _producer;

        public AutoRecoveringProducer(string address, RoutingType routingType)
        {
            _address = address;
            _routingType = routingType;
        }

        public Task ProduceAsync(Message message)
        {
            return _producer.ProduceAsync(message);
        }

        public void Produce(Message message)
        {
            _producer.Produce(message);
        }

        public async ValueTask DisposeAsync()
        {
            await _producer.DisposeAsync().ConfigureAwait(false);
            Closed?.Invoke(this);
        }

        public Task RecoverAsync(IConnection connection)
        {
            _producer = connection.CreateProducer(_address, _routingType);
            return Task.CompletedTask;
        }

        public event Closed Closed;
    }
}