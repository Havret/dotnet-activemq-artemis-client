﻿using System;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Net.Builders;
using Amqp;
using Amqp.Framing;
using Microsoft.Extensions.Logging;

namespace ActiveMQ.Net
{
    internal class Connection : IConnection
    {
        private readonly Amqp.Connection _connection;
        private readonly ILoggerFactory _loggerFactory;
        private readonly Session _session;
        private bool _closed;
        private Error _error;

        public Connection(ILoggerFactory loggerFactory, Amqp.Connection connection, Session session)
        {
            _loggerFactory = loggerFactory;
            _connection = connection;
            _session = session;
            _connection.AddClosedCallback(OnConnectionClosed);
        }

        public bool IsOpened => _connection.ConnectionState == ConnectionState.Opened;

        public Task<IConsumer> CreateConsumerAsync(string address, RoutingType routingType, CancellationToken cancellationToken)
        {
            var consumerBuilder = new ConsumerBuilder(_loggerFactory, _session);
            return consumerBuilder.CreateAsync(address, routingType, cancellationToken);
        }

        public Task<IProducer> CreateProducerAsync(string address, RoutingType routingType)
        {
            var producerBuilder = new ProducerBuilder(_loggerFactory, _session);
            return producerBuilder.CreateAsync(address, routingType);
        }

        public async ValueTask DisposeAsync()
        {
            await _connection.CloseAsync().ConfigureAwait(false);
            _connection.Closed -= OnConnectionClosed;
        }

        public event EventHandler<ConnectionClosedEventArgs> ConnectionClosed
        {
            add
            {
                if (!_closed)
                {
                    _connectionClosed += value;
                    return;
                }

                value(this, GetConnectionClosedEventArgs());
            }
            remove => _connectionClosed -= value;
        }

        private event EventHandler<ConnectionClosedEventArgs> _connectionClosed;

        private void OnConnectionClosed(IAmqpObject sender, Error error)
        {
            _error = error;
            _closed = true;
            _connectionClosed?.Invoke(this, GetConnectionClosedEventArgs());
        }

        private ConnectionClosedEventArgs GetConnectionClosedEventArgs()
        {
            return new ConnectionClosedEventArgs(_error != null, _error?.ToString());
        }
    }
}