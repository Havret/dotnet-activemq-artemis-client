using System;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Transactions;
using Microsoft.Extensions.Logging;

namespace ActiveMQ.Artemis.Client.Extensions.DependencyInjection
{
    internal class TypedActiveMqAnonymousProducer<T> : IAnonymousProducer, IActiveMqProducer
    {
        private readonly ILogger<TypedActiveMqAnonymousProducer<T>> _logger;
        private IAnonymousProducer _producer;
        private readonly Func<CancellationToken, Task<IAnonymousProducer>> _producerFactory;
        private readonly SendObservable _sendObservable;

        public TypedActiveMqAnonymousProducer(ILogger<TypedActiveMqAnonymousProducer<T>> logger, Func<CancellationToken, Task<IAnonymousProducer>> producerFactory, SendObservable sendObservable)
        {
            _logger = logger;
            _producerFactory = producerFactory;
            _sendObservable = sendObservable;
        }

        public async ValueTask StartAsync(CancellationToken cancellationToken)
        {
            if (_producer != null)
            {
                throw new InvalidOperationException($"Producer with type {typeof(T).FullName} has already been initialized.");
            }

            _producer = await _producerFactory(cancellationToken);
        }

        public async ValueTask StopAsync()
        {
            try
            {
                await DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Exception occured during producer stopping.");
            }
            finally
            {
                _producer = null;                
            }
        }

        public async Task SendAsync(string address, RoutingType? routingType, Message message, Transaction transaction, CancellationToken cancellationToken)
        {
            CheckState();
            
            _sendObservable.PreSend(address, routingType, message);
            await _producer.SendAsync(address, routingType, message, transaction, cancellationToken).ConfigureAwait(false);
            _sendObservable.PostSend(address, routingType, message);
        }

        public void Send(string address, RoutingType? routingType, Message message, CancellationToken cancellationToken)
        {
            CheckState();
            
            _sendObservable.PreSend(address, routingType, message);
            _producer.Send(address, routingType, message, cancellationToken);
            _sendObservable.PostSend(address, routingType, message);
        }
        
        private void CheckState()
        {
            if (_producer == null)
            {
                throw new InvalidOperationException("Producer was not started.");
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_producer != null)
            {
                await _producer.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}