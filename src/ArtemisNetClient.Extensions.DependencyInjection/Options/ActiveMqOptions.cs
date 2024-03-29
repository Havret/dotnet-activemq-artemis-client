﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ActiveMQ.Artemis.Client.Extensions.DependencyInjection
{
    internal class ActiveMqOptions
    {
        public bool EnableQueueDeclaration { get; set; }
        public bool EnableAddressDeclaration { get; set; }
        public List<QueueConfiguration> QueueConfigurations { get; } = new List<QueueConfiguration>();
        public Dictionary<string, HashSet<RoutingType>> AddressConfigurations { get; } = new Dictionary<string, HashSet<RoutingType>>();
        public List<Action<IServiceProvider, ConnectionFactory>> ConnectionFactoryActions { get; } = new List<Action<IServiceProvider, ConnectionFactory>>();
        public List<Func<IServiceProvider, ITopologyManager, Task>> ConfigureTopologyActions { get; } = new List<Func<IServiceProvider, ITopologyManager, Task>>();
        public List<Action<IServiceProvider, IConnection>> ConnectionActions { get; } = new List<Action<IServiceProvider, IConnection>>();
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