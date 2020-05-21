using System;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.MessageIdPolicy;
using ActiveMQ.Artemis.Client.Transactions;
using Amqp;
using Microsoft.Extensions.Logging;

namespace ActiveMQ.Artemis.Client
{
    internal class Producer : ProducerBase, IProducer
    {
        private readonly ProducerConfiguration _configuration;

        public Producer(ILoggerFactory loggerFactory, SenderLink senderLink, TransactionsManager transactionsManager, ProducerConfiguration configuration, Func<IMessageIdPolicy> messageIdPolicyFactory) :
            base(loggerFactory, senderLink, transactionsManager, configuration)
        {
            _configuration = configuration;
        }

        public Task SendAsync(Message message, Transaction transaction, CancellationToken cancellationToken = default)
        {
            return SendInternalAsync(_configuration.Address, _configuration.RoutingType, message, transaction, cancellationToken);
        }

        public void Send(Message message)
        {
            SendInternal(_configuration.Address, _configuration.RoutingType, message);
        }
    }
}