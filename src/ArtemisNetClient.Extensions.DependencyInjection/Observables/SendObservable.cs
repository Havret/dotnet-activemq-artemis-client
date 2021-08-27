namespace ActiveMQ.Artemis.Client.Extensions.DependencyInjection
{
    internal class SendObservable
    {
        private readonly ISendObserver[] _sendObservers;

        public SendObservable(string name, ISendObserver[] sendObservers)
        {
            _sendObservers = sendObservers;
            Name = name;
        }

        public string Name { get; }

        public void PreSend(string address, RoutingType? routingType, Message message)
        {
            foreach (var sendObserver in _sendObservers)
            {
                sendObserver.PreSend(address, routingType, message);
            }
        }

        public void PostSend(string address, RoutingType? routingType, Message message)
        {
            foreach (var sendObserver in _sendObservers)
            {
                sendObserver.PostSend(address, routingType, message);
            }
        }
    }
}