using System;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Builders;
using ActiveMQ.Artemis.Client.MessageIdPolicy;
using ActiveMQ.Artemis.Client.Transactions;
using Amqp;
using Amqp.Framing;
using Microsoft.Extensions.Logging;

namespace ActiveMQ.Artemis.Client
{
    internal class Connection : IConnection
    {
        private readonly Amqp.Connection _connection;
        private readonly Func<IMessageIdPolicy> _messageIdPolicyFactory;
        private readonly ILoggerFactory _loggerFactory;
        private readonly TransactionsManager _transactionsManager;
        private bool _closed;
        private Error _error;

        public Connection(ILoggerFactory loggerFactory, Endpoint endpoint, Amqp.Connection connection, Func<IMessageIdPolicy> messageIdPolicyFactory)
        {
            _loggerFactory = loggerFactory;
            Endpoint = endpoint;
            _connection = connection;
            _messageIdPolicyFactory = messageIdPolicyFactory;
            _connection.AddClosedCallback(OnConnectionClosed);
            _transactionsManager = new TransactionsManager(this);
        }

        public Endpoint Endpoint { get; }
        public bool IsOpened => _connection.ConnectionState == ConnectionState.Opened;

        public async Task<IConsumer> CreateConsumerAsync(ConsumerConfiguration configuration, CancellationToken cancellationToken)
        {
            var session = await CreateSession(cancellationToken).ConfigureAwait(false);
            var consumerBuilder = new ConsumerBuilder(_loggerFactory, _transactionsManager, session);
            return await consumerBuilder.CreateAsync(configuration, cancellationToken).ConfigureAwait(false);
        }

        public async Task<IProducer> CreateProducerAsync(ProducerConfiguration configuration, CancellationToken cancellationToken)
        {
            var session = await CreateSession(cancellationToken).ConfigureAwait(false);
            var producerBuilder = new ProducerBuilder(_loggerFactory, _transactionsManager, session, _messageIdPolicyFactory);
            return await producerBuilder.CreateAsync(configuration, cancellationToken).ConfigureAwait(false);
        }

        public async Task<IAnonymousProducer> CreateAnonymousProducer(AnonymousProducerConfiguration configuration, CancellationToken cancellationToken)
        {
            var session = await CreateSession(cancellationToken).ConfigureAwait(false);
            var producerBuilder = new AnonymousProducerBuilder(_loggerFactory, _transactionsManager, session);
            return await producerBuilder.CreateAsync(configuration, cancellationToken).ConfigureAwait(false);
        }

        internal async Task<TransactionCoordinator> CreateTransactionCoordinator(CancellationToken cancellationToken)
        {
            var session = await CreateSession(cancellationToken).ConfigureAwait(false);
            var transactionCoordinatorBuilder = new TransactionCoordinatorBuilder(session);
            return await transactionCoordinatorBuilder.CreateAsync(cancellationToken).ConfigureAwait(false);
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