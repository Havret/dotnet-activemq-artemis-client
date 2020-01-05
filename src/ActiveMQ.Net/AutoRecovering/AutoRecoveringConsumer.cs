using System.Threading.Tasks;

namespace ActiveMQ.Net.AutoRecovering
{
    internal class AutoRecoveringConsumer : IConsumer, IRecoverable
    {
        private readonly string _address;
        private readonly RoutingType _routingType;
        private IConsumer _consumer;

        public AutoRecoveringConsumer(string address, RoutingType routingType)
        {
            _address = address;
            _routingType = routingType;
        }

        public ValueTask<Message> ConsumeAsync()
        {
            return _consumer.ConsumeAsync();
        }

        public void Accept(Message message)
        {
            _consumer.Accept(message);
        }

        public void Reject(Message message)
        {
            _consumer.Reject(message);
        }

        public async ValueTask DisposeAsync()
        {
            await _consumer.DisposeAsync().ConfigureAwait(false);
            Closed?.Invoke(this);
        }

        public async Task RecoverAsync(IConnection connection)
        {
            _consumer = await connection.CreateConsumerAsync(_address, _routingType).ConfigureAwait(false);
        }

        public event Closed Closed;
    }
}