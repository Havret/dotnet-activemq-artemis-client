using System;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Net.Exceptions;
using Amqp;
using Amqp.Framing;
using Microsoft.Extensions.Logging;

namespace ActiveMQ.Net.Builders
{
    internal class ConsumerBuilder
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly Session _session;
        private readonly TaskCompletionSource<bool> _tcs;

        public ConsumerBuilder(ILoggerFactory loggerFactory, Session session)
        {
            _loggerFactory = loggerFactory;
            _session = session;
            _tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public async Task<IConsumer> CreateAsync(ConsumerConfiguration configuration, CancellationToken cancellationToken)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            if (string.IsNullOrWhiteSpace(configuration.Address)) throw new ArgumentNullException(nameof(configuration.Address), "The address cannot be empty.");

            cancellationToken.ThrowIfCancellationRequested();
            cancellationToken.Register(() => _tcs.TrySetCanceled());

            var routingCapability = configuration.RoutingType.GetRoutingCapability();
            var source = new Source
            {
                Address = GetAddress(configuration.Address, configuration.Queue),
                Capabilities = new[] { routingCapability },
            };

            var receiverLink = new ReceiverLink(_session, Guid.NewGuid().ToString(), source, OnAttached);
            receiverLink.AddClosedCallback(OnClosed);
            await _tcs.Task.ConfigureAwait(false);
            receiverLink.Closed -= OnClosed;
            return new Consumer(_loggerFactory, receiverLink);
        }

        private static string GetAddress(string address, string queue)
        {
            return string.IsNullOrEmpty(queue)
                ? address
                : CreateFullyQualifiedQueueName(address, queue);
        }

        private static string CreateFullyQualifiedQueueName(string address, string queue)
        {
            return $"{address}::{queue}";
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
                _tcs.TrySetException(CreateConsumerException.FromError(error));
            }
        }
    }
}