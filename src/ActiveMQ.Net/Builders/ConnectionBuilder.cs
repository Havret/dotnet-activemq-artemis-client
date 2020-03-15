using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Net.Exceptions;
using Amqp;
using Amqp.Framing;
using Microsoft.Extensions.Logging;

namespace ActiveMQ.Net.Builders
{
    internal class ConnectionBuilder
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly TaskCompletionSource<bool> _tcs;

        public ConnectionBuilder(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
            _tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public async Task<IConnection> CreateAsync(Endpoint endpoint, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            cancellationToken.Register(() => _tcs.TrySetCanceled());

            var connectionFactory = new Amqp.ConnectionFactory();
            var connection = await connectionFactory.CreateAsync(endpoint.Address, null, OnOpened).ConfigureAwait(false);
            connection.AddClosedCallback(OnClosed);
            await _tcs.Task.ConfigureAwait(false);
            connection.Closed -= OnClosed;
            var session = new Session(connection);
            return new Connection(_loggerFactory, endpoint, connection, session);
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
                _tcs.TrySetException(CreateConnectionException.FromError(error));
            }
        }
    }
}