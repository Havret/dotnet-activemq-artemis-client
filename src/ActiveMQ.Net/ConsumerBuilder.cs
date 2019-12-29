using System;
using System.Threading.Tasks;
using Amqp;
using Amqp.Framing;

namespace ActiveMQ.Net
{
    internal class ConsumerBuilder
    {
        private readonly Session _session;
        private readonly TaskCompletionSource<IConsumer> _tcs;

        public ConsumerBuilder(Session session)
        {
            _session = session;
            _tcs = new TaskCompletionSource<IConsumer>();
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
            var consumer = await _tcs.Task;
            receiverLink.Closed -= OnClosed;

            return consumer;
        }

        private void OnAttached(ILink link, Attach attach)
        {
            if (attach.Source != null)
            {
                _tcs.TrySetResult(new Consumer(link as ReceiverLink));
            }
        }
        
        private void OnClosed(IAmqpObject sender, Error error)
        {
            if (error != null)
            {
                _tcs.TrySetException(CannotCreateConsumerException.FromError(error));
            }
        }
    }
}