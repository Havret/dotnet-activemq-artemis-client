using System;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Extensions.DependencyInjection.InternalUtils;
using ActiveMQ.Artemis.Client.Transactions;
using Microsoft.Extensions.Logging;

namespace ActiveMQ.Artemis.Client.Extensions.DependencyInjection
{
    internal class TypedActiveMqAnonymousProducer<T> : IAnonymousProducer, IActiveMqProducer
    {
        private readonly ILogger<TypedActiveMqAnonymousProducer<T>> _logger;
        private AsyncValueLazy<IAnonymousProducer> _producer;
        private readonly Func<CancellationToken, Task<IAnonymousProducer>> _producerFactory;
        private readonly SendObservable _sendObservable;

        public TypedActiveMqAnonymousProducer(ILogger<TypedActiveMqAnonymousProducer<T>> logger, Func<CancellationToken, Task<IAnonymousProducer>> producerFactory, SendObservable sendObservable)
        {
            _logger = logger;
            _producerFactory = producerFactory;
            _sendObservable = sendObservable;
            _producer = new AsyncValueLazy<IAnonymousProducer>(producerFactory);
        }

        public async ValueTask StartAsync(CancellationToken cancellationToken)
        {
            _producer ??= new AsyncValueLazy<IAnonymousProducer>(_producerFactory);
            await _producer.GetValueAsync(cancellationToken).ConfigureAwait(false);
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
            var producer = await _producer.GetValueAsync(cancellationToken).ConfigureAwait(false);
            
            _sendObservable.PreSend(address, routingType, message);
            await producer.SendAsync(address, routingType, message, transaction, cancellationToken).ConfigureAwait(false);
            _sendObservable.PostSend(address, routingType, message);
        }

        public void Send(string address, RoutingType? routingType, Message message, CancellationToken cancellationToken)
        {
            CheckState();
            var producer = _producer.GetValue(cancellationToken);
            
            _sendObservable.PreSend(address, routingType, message);
            producer.Send(address, routingType, message, cancellationToken);
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
            if (_producer != null && _producer.TryGetValue(out var producer))
            {
                await producer.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}