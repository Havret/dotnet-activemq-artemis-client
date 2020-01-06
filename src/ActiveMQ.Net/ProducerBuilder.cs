using System;
using System.Threading.Tasks;
using Amqp;
using Amqp.Framing;

namespace ActiveMQ.Net
{
    public class ProducerBuilder
    {
        private readonly Session _session;
        private readonly TaskCompletionSource<IProducer> _tcs;

        public ProducerBuilder(Session session)
        {
            _session = session;
            _tcs = new TaskCompletionSource<IProducer>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public async Task<IProducer> CreateAsync(string address, RoutingType routingType)
        {
            var routingCapability = routingType.GetRoutingCapability();
            var target = new Target
            {
                Address = address,
                Capabilities = new[] { routingCapability }
            };
            var senderLink = new SenderLink(_session, Guid.NewGuid().ToString(), target , OnAttached);
            senderLink.AddClosedCallback(OnClosed);
            var producer = await _tcs.Task.ConfigureAwait(false);
            senderLink.Closed -= OnClosed;
            return producer;
        }
        
        private void OnAttached(ILink link, Attach attach)
        {
            if (attach.Source != null)
            {
                _tcs.TrySetResult(new Producer(link as SenderLink));
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