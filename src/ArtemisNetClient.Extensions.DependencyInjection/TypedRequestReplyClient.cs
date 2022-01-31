using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ActiveMQ.Artemis.Client.Extensions.DependencyInjection
{
    internal class TypedRequestReplyClient<T> : IRequestReplyClient, IActiveMqRequestReplyClient
    {
        private readonly ILogger<TypedRequestReplyClient<T>> _logger;
        private readonly Func<CancellationToken, Task<IRequestReplyClient>> _requestReplyClientFactory;
        private readonly SendObservable _sendObservable;
        private IRequestReplyClient _requestReplyClient;

        public TypedRequestReplyClient(ILogger<TypedRequestReplyClient<T>> logger,
            Func<CancellationToken, Task<IRequestReplyClient>> requestReplyClientFactory,
            SendObservable sendObservable)
        {
            _logger = logger;
            _requestReplyClientFactory = requestReplyClientFactory;
            _sendObservable = sendObservable;
        }

        public async Task<Message> SendAsync(string address, RoutingType? routingType, Message message, CancellationToken cancellationToken)
        {
            CheckState();

            _sendObservable.PreSend(address, routingType, message);
            var response = await _requestReplyClient.SendAsync(address, routingType, message, cancellationToken).ConfigureAwait(false);
            _sendObservable.PostSend(address, routingType, message);
            return response;
        }

        private void CheckState()
        {
            if (_requestReplyClient == null)
            {
                throw new InvalidOperationException("Request Reply Client was not started.");
            }
        }

        public async ValueTask StartAsync(CancellationToken cancellationToken)
        {
            if (_requestReplyClient != null)
            {
                throw new InvalidOperationException($"Request Reply Client with type {typeof(T).FullName} has already been initialized.");
            }

            _requestReplyClient = await _requestReplyClientFactory(cancellationToken).ConfigureAwait(false);
        }

        public async ValueTask StopAsync()
        {
            try
            {
                await DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Exception occured during Request Reply Client stopping");
            }
            finally
            {
                _requestReplyClient = null;
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_requestReplyClient != null)
            {
                await _requestReplyClient.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}