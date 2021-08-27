namespace ActiveMQ.Artemis.Client.Extensions.DependencyInjection
{
    internal class ReceiveObservable
    {
        private readonly IReceiveObserver[] _receiveObservers;

        public ReceiveObservable(string name, IReceiveObserver[] receiveObservers)
        {
            _receiveObservers = receiveObservers;
            Name = name;
        }

        public string Name { get; }

        public void PreReceive(string address, RoutingType routingType, string queue, Message message)
        {
            foreach (var receiveObserver in _receiveObservers)
            {
                receiveObserver.PreReceive(address, routingType, queue, message);
            }
        }

        public void PostReceive(string address, RoutingType routingType, string queue, Message message)
        {
            foreach (var receiveObserver in _receiveObservers)
            {
                receiveObserver.PostReceive(address, routingType, queue, message);
            }
        }
    }
}