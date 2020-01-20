using System;
using System.Threading.Tasks;
using Amqp;
using Amqp.Framing;
using Microsoft.Extensions.Logging;

namespace ActiveMQ.Net.Builders
{
    internal class ConsumerBuilder
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly Session _session;
        private readonly TaskCompletionSource<IConsumer> _tcs;

        public ConsumerBuilder(ILoggerFactory loggerFactory, Session session)
        {
            _loggerFactory = loggerFactory;
            _session = session;
            _tcs = new TaskCompletionSource<IConsumer>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public async Task<IConsumer> CreateAsync(string address, RoutingType routingType)
        {
            var routingCapability = routingType.GetRoutingCapability();
            var source = new Source
            {
                Address = address,
                Capabilities = new[] { routingCapability }
            };
            var receiverLink = new ReceiverLink(_session, Guid.NewGuid().ToString(), source, OnAttached);
            receiverLink.AddClosedCallback(OnClosed);
            var consumer = await _tcs.Task.ConfigureAwait(false);
            receiverLink.Closed -= OnClosed;

            return consumer;
        }

        private void OnAttached(ILink link, Attach attach)
        {
            if (attach.Source != null)
            {
                _tcs.TrySetResult(new Consumer(_loggerFactory, link as ReceiverLink));
            }
        }
        
        private void OnClosed(IAmqpObject sender, Error error)
        {
            if (error != null)
            {
                _tcs.TrySetException(CreateConsumerException.FromError(error));
            }
        }
    }
}