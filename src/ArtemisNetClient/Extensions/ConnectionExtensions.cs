﻿using System.Threading;
using System.Threading.Tasks;

namespace ActiveMQ.Artemis.Client
{
    public static class ConnectionExtensions
    {
        public static Task<IConsumer> CreateConsumerAsync(this IConnection connection, string address, string queue, CancellationToken cancellationToken = default)
        {
            var configuration = new ConsumerConfiguration
            {
                Address = address,
                Queue = queue
            };
            return connection.CreateConsumerAsync(configuration, cancellationToken);
        }
        
        public static Task<IConsumer> CreateConsumerAsync(this IConnection connection, string address, RoutingType routingType, CancellationToken cancellationToken = default)
        {
            var configuration = new ConsumerConfiguration
            {
                Address = address,
                RoutingType = routingType
            };
            return connection.CreateConsumerAsync(configuration, cancellationToken);
        }

        public static Task<IProducer> CreateProducerAsync(this IConnection connection, string address, CancellationToken cancellationToken = default)
        {
            var configuration = new ProducerConfiguration
            {
                Address = address,
            };            
            return connection.CreateProducerAsync(configuration, cancellationToken);
        }

        public static Task<IProducer> CreateProducerAsync(this IConnection connection, string address, RoutingType routingType, CancellationToken cancellationToken = default)
        {
            var configuration = new ProducerConfiguration
            {
                Address = address,
                RoutingType = routingType
            };            
            return connection.CreateProducerAsync(configuration, cancellationToken);
        }

        public static Task<IAnonymousProducer> CreateAnonymousProducerAsync(this IConnection connection, CancellationToken cancellationToken = default)
        {
            var configuration = new AnonymousProducerConfiguration();
            return connection.CreateAnonymousProducerAsync(configuration, cancellationToken);
        }
    }
}