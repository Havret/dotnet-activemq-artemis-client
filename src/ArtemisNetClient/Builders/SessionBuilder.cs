using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Exceptions;
using Amqp;
using Amqp.Framing;

namespace ActiveMQ.Artemis.Client.Builders
{
    internal class SessionBuilder
    {
        private const uint DefaultWindowSize = 2048;
        private const int DefaultMaxLinksPerSession = 63;

        private readonly Amqp.Connection _connection;
        private readonly TaskCompletionSource<bool> _tcs;

        public SessionBuilder(Amqp.Connection connection)
        {
            _connection = connection;
            _tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public async Task<Session> CreateAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var _ = cancellationToken.Register(() => _tcs.TrySetCanceled());

            var begin = new Begin
            {
                IncomingWindow = DefaultWindowSize,
                OutgoingWindow = DefaultWindowSize,
                HandleMax = DefaultMaxLinksPerSession,
                NextOutgoingId = uint.MaxValue - 2u
            };

            var session = new Session(_connection, begin, OnBegin);
            session.AddClosedCallback(OnClosed);

            // A session created just as the connection ends is never notified, the connection only ends the sessions
            // it already had. Checked after subscribing, so that the connection ending at any point is noticed.
            _connection.AddClosedCallback(OnClosed);
            if (_connection.IsClosed)
            {
                OnClosed(_connection, _connection.Error);
            }

            try
            {
                await _tcs.Task.ConfigureAwait(false);
            }
            finally
            {
                session.Closed -= OnClosed;
                _connection.Closed -= OnClosed;
            }

            return session;
        }

        private void OnBegin(ISession session, Begin begin)
        {
            _tcs.TrySetResult(true);
        }
        
        private void OnClosed(IAmqpObject sender, Error error)
        {
            // The error is null when the connection was closed cleanly, which still means the session will never begin
            _tcs.TrySetException(error != null
                ? new CreateSessionException(error.Description, error.Condition)
                : new CreateSessionException("The session was closed before it was established.", null));
        }
    }
}