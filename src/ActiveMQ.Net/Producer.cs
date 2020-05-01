using System.Threading;
using System.Threading.Tasks;
using Amqp;
using Microsoft.Extensions.Logging;

namespace ActiveMQ.Net
{
    internal class Producer : ProducerBase, IProducer
    {
        private readonly ProducerConfiguration _configuration;

        public Producer(ILoggerFactory loggerFactory, SenderLink senderLink, ProducerConfiguration configuration) : base(loggerFactory, senderLink, configuration)
        {
            _configuration = configuration;
        }

        public Task SendAsync(Message message, CancellationToken cancellationToken = default)
        {
            return SendInternalAsync(_configuration.Address, _configuration.RoutingType, message, cancellationToken);
        }

        public void Send(Message message)
        {
            SendInternal(_configuration.Address, _configuration.RoutingType, message);
        }
    }
}