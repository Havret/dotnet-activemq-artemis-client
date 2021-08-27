namespace ActiveMQ.Artemis.Client.Extensions.DependencyInjection
{
    internal class ContextualSendObservable
    {
        private readonly SendObservable _innerObservable;

        public ContextualSendObservable(SendObservable innerObservable)
        {
            _innerObservable = innerObservable;
        }
        
        public string Address { get; set; }
        public RoutingType? RoutingType { get; set; }

        public void PreSend(Message message)
        {
            _innerObservable.PreSend(Address, RoutingType, message);
        }

        public void PostSend(Message message)
        {
            _innerObservable.PostSend(Address, RoutingType, message);
        }
    }
}