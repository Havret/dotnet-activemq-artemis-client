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
            cancellationToken.Register(() => _tcs.TrySetCanceled());

            var begin = new Begin
            {
                IncomingWindow = DefaultWindowSize,
                OutgoingWindow = DefaultWindowSize,
                HandleMax = DefaultMaxLinksPerSession,
                NextOutgoingId = uint.MaxValue - 2u
            };

            var session = new Session(_connection, begin, OnBegin);
            session.AddClosedCallback(OnClosed);
            await _tcs.Task.ConfigureAwait(false);
            session.Closed -= OnClosed;
            return session;
        }

        private void OnBegin(ISession session, Begin begin)
        {
            _tcs.TrySetResult(true);
        }
        
        private void OnClosed(IAmqpObject sender, Error error)
        {
            if (error != null)
            {
                _tcs.TrySetException(new CreateSessionException(error.Description, error.Condition));
            }
        }
    }
}