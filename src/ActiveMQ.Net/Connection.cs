using System;
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

        public Connection(ILoggerFactory loggerFactory, Endpoint endpoint, Amqp.Connection connection, Session session)
        {
            _loggerFactory = loggerFactory;
            Endpoint = endpoint;
            _connection = connection;
            _session = session;
            _connection.AddClosedCallback(OnConnectionClosed);
        }

        public Endpoint Endpoint { get; }
        public bool IsOpened => _connection.ConnectionState == ConnectionState.Opened;

        public Task<IConsumer> CreateConsumerAsync(string address, RoutingType routingType, CancellationToken cancellationToken)
        {
            var consumerBuilder = new ConsumerBuilder(_loggerFactory, _session);
            return consumerBuilder.CreateAsync(address, routingType, cancellationToken);
        }

        public Task<IProducer> CreateProducerAsync(string address, RoutingType routingType, CancellationToken cancellationToken)
        {
            var producerBuilder = new ProducerBuilder(_loggerFactory, _session);
            return producerBuilder.CreateAsync(address, routingType, cancellationToken);
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

        // ReSharper disable once InconsistentNaming
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

#pragma warning disable CS0067
        public event EventHandler<ConnectionRecoveredEventArgs> ConnectionRecovered;
#pragma warning restore CS0067
    }
}