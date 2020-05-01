using System;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Net.Exceptions;
using Amqp;
using Amqp.Framing;
using Microsoft.Extensions.Logging;

namespace ActiveMQ.Net.Builders
{
    public class AnonymousProducerBuilder
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly Session _session;
        private readonly TaskCompletionSource<bool> _tcs;

        public AnonymousProducerBuilder(ILoggerFactory loggerFactory, Session session)
        {
            _loggerFactory = loggerFactory;
            _session = session;
            _tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public async Task<IAnonymousProducer> CreateAsync(AnonymousProducerConfiguration configuration, CancellationToken cancellationToken)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            
            cancellationToken.ThrowIfCancellationRequested();
            cancellationToken.Register(() => _tcs.TrySetCanceled());

            var target = new Target
            {
                Address = null,
            };
            var senderLink = new SenderLink(_session, Guid.NewGuid().ToString(), target, OnAttached);
            senderLink.AddClosedCallback(OnClosed);
            await _tcs.Task.ConfigureAwait(false);
            var producer = new AnonymousProducer(_loggerFactory, senderLink, configuration);
            senderLink.Closed -= OnClosed;
            return producer;
        }
        
        private void OnAttached(ILink link, Attach attach)
        {
            if (attach.Source != null)
            {
                _tcs.TrySetResult(true);
            }
        }
        
        private void OnClosed(IAmqpObject sender, Error error)
        {
            if (error != null)
            {
                _tcs.TrySetException(CreateProducerException.FromError(error));
            }
        }
    }
}