using System;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Exceptions;
using ActiveMQ.Artemis.Client.MessageIdPolicy;
using Amqp;
using Amqp.Framing;
using Microsoft.Extensions.Logging;

namespace ActiveMQ.Artemis.Client.Builders
{
    internal class ConnectionBuilder
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly Func<IMessageIdPolicy> _messageIdPolicyFactory;
        private readonly TaskCompletionSource<bool> _tcs;

        public ConnectionBuilder(ILoggerFactory loggerFactory, Func<IMessageIdPolicy> messageIdPolicyFactory)
        {
            _loggerFactory = loggerFactory;
            _messageIdPolicyFactory = messageIdPolicyFactory;
            _tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public async Task<IConnection> CreateAsync(Endpoint endpoint, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            cancellationToken.Register(() => _tcs.TrySetCanceled());

            var connectionFactory = new Amqp.ConnectionFactory();
            try
            {
                var connection = await connectionFactory.CreateAsync(endpoint.Address, null, OnOpened).ConfigureAwait(false);
                connection.AddClosedCallback(OnClosed);
                await _tcs.Task.ConfigureAwait(false);
                connection.Closed -= OnClosed;
                return new Connection(_loggerFactory, endpoint, connection, _messageIdPolicyFactory);
            }
            catch (AmqpException exception)
            {
                throw new CreateConnectionException(message: exception.Error?.Description ?? exception.Message, errorCode: exception.Error?.Condition);
            }
        }

        private void OnOpened(Amqp.IConnection connection, Open open)
        {
            if (connection != null)
            {
                _tcs.TrySetResult(true);
            }
        }

        private void OnClosed(IAmqpObject sender, Error error)
        {
            if (error != null)
            {
                _tcs.TrySetException(new CreateConnectionException(error.Description, error.Condition));
            }
        }
    }
}