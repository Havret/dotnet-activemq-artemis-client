namespace ActiveMQ.Artemis.Client.Extensions.DependencyInjection
{
    internal class ContextualReceiveObservable
    {
        public string Address { get; set; }
        public RoutingType RoutingType { get; set; }
        public string Queue { get; set; }
        
        private readonly ReceiveObservable _innerObservable;

        public ContextualReceiveObservable(ReceiveObservable innerObservable)
        {
            _innerObservable = innerObservable;
        }

        public void PreReceive(Message message)
        {
            _innerObservable.PreReceive(Address, RoutingType, Queue, message);
        }

        public void PostReceive(Message message)
        {
            _innerObservable.PostReceive(Address, RoutingType, Queue, message);
        }
    }
}