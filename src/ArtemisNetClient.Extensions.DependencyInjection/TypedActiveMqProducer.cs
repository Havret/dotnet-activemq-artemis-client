using System;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Extensions.DependencyInjection.InternalUtils;
using ActiveMQ.Artemis.Client.Transactions;
using Microsoft.Extensions.Logging;

namespace ActiveMQ.Artemis.Client.Extensions.DependencyInjection
{
    internal class TypedActiveMqProducer<T> : IProducer, IActiveMqProducer
    {
        private AsyncValueLazy<IProducer> _producer;
        private readonly ILogger<TypedActiveMqProducer<T>> _logger;
        private readonly Func<CancellationToken, Task<IProducer>> _producerFactory;
        private readonly ContextualSendObservable _sendObservable;

        public TypedActiveMqProducer(ILogger<TypedActiveMqProducer<T>> logger, Func<CancellationToken, Task<IProducer>> producerFactory, ContextualSendObservable sendObservable)
        {
            _logger = logger;
            _producerFactory = producerFactory;
            _sendObservable = sendObservable;
            _producer = new AsyncValueLazy<IProducer>(producerFactory);
        }

        async ValueTask IActiveMqProducer.StartAsync(CancellationToken cancellationToken)
        {
            _producer ??= new AsyncValueLazy<IProducer>(_producerFactory);
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
                _logger.LogError(exception, "Exception occured during producer stopping");
            }
            finally
            {
                _producer = null;                
            }
        }

        public async Task SendAsync(Message message, Transaction transaction, CancellationToken cancellationToken)
        {
            CheckState();
            var producer = await _producer.GetValueAsync(cancellationToken).ConfigureAwait(false);
            _sendObservable.PreSend(message);
            await producer.SendAsync(message, transaction, cancellationToken).ConfigureAwait(false);
            _sendObservable.PostSend(message);
        }

        public void Send(Message message, CancellationToken cancellationToken)
        {
            CheckState();
            var producer = _producer.GetValue(cancellationToken);
            _sendObservable.PreSend(message);
            producer.Send(message, cancellationToken);
            _sendObservable.PostSend(message);
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