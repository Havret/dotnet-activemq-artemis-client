using System;
using System.Collections.Generic;

namespace ActiveMQ.Artemis.Client.Extensions.DependencyInjection
{
    internal class ActiveMqOptions
    {
        public bool EnableQueueDeclaration { get; set; }
        public bool EnableAddressDeclaration { get; set; }
        public List<QueueConfiguration> QueueConfigurations { get; } = new List<QueueConfiguration>();
        public Dictionary<string, HashSet<RoutingType>> AddressConfigurations { get; set; } = new Dictionary<string, HashSet<RoutingType>>();
        public List<Action<IServiceProvider, ConnectionFactory>> ConnectionFactoryActions { get; } = new List<Action<IServiceProvider, ConnectionFactory>>();
        public List<SendObserverRegistration> SendObserverRegistrations { get; } = new List<SendObserverRegistration>();
        public List<ReceiveObserverRegistration> ReceiveObserverRegistrations { get; } = new List<ReceiveObserverRegistration>();
    }

    internal class ReceiveObserverRegistration
    {
        public Type Type { get; set; }
        public Func<IServiceProvider, IReceiveObserver> ImplementationFactory { get; set; }
    }
    
    internal class SendObserverRegistration
    {
        public Type Type { get; set; }
        public Func<IServiceProvider, ISendObserver> ImplementationFactory { get; set; }
    }
}