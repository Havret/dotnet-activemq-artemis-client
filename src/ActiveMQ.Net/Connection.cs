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
        private bool _closed;
        private Error _error;

        public Connection(ILoggerFactory loggerFactory, Endpoint endpoint, Amqp.Connection connection)
        {
            _loggerFactory = loggerFactory;
            Endpoint = endpoint;
            _connection = connection;
            _connection.AddClosedCallback(OnConnectionClosed);
        }

        public Endpoint Endpoint { get; }
        public bool IsOpened => _connection.ConnectionState == ConnectionState.Opened;

        public async Task<IConsumer> CreateConsumerAsync(ConsumerConfiguration configuration, CancellationToken cancellationToken)
        {
            var session = await CreateSession(cancellationToken).ConfigureAwait(false);
            var consumerBuilder = new ConsumerBuilder(_loggerFactory, session);
            return await consumerBuilder.CreateAsync(configuration, cancellationToken).ConfigureAwait(false);
        }

        public async Task<IProducer> CreateProducerAsync(ProducerConfiguration configuration, CancellationToken cancellationToken)
        {
            var session = await CreateSession(cancellationToken).ConfigureAwait(false);
            var producerBuilder = new ProducerBuilder(_loggerFactory, session);
            return await producerBuilder.CreateAsync(configuration, cancellationToken).ConfigureAwait(false);
        }

        public async Task<IAnonymousProducer> CreateAnonymousProducer(CancellationToken cancellationToken)
        {
            var session = await CreateSession(cancellationToken).ConfigureAwait(false);
            var producerBuilder = new AnonymousProducerBuilder(_loggerFactory, session);
            return await producerBuilder.CreateAsync(cancellationToken).ConfigureAwait(false);
        }

        private Task<Session> CreateSession(CancellationToken cancellationToken)
        {
            var sessionBuilder = new SessionBuilder(_connection);
            return sessionBuilder.CreateAsync(cancellationToken);
        }

        public async ValueTask DisposeAsync()
        {
            if (!_closed)
            {
                await _connection.CloseAsync().ConfigureAwait(false);
            }

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
        public event EventHandler<ConnectionRecoveryErrorEventArgs> ConnectionRecoveryError;
#pragma warning restore CS0067
    }
}