using System;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Transactions;
using Amqp;
using Microsoft.Extensions.Logging;

namespace ActiveMQ.Artemis.Client
{
    internal class AnonymousProducer : ProducerBase, IAnonymousProducer
    {
        public AnonymousProducer(ILoggerFactory loggerFactory, SenderLink senderLink, TransactionsManager transactionsManager, IBaseProducerConfiguration configuration) :
            base(loggerFactory, senderLink, transactionsManager, configuration)
        {
        }

        public Task SendAsync(string address, RoutingType? routingType, Message message, Transaction transaction, CancellationToken cancellationToken)
        {
            CheckAddress(address);
            CheckMessage(message);

            return SendInternalAsync(address, routingType, message, transaction, cancellationToken);
        }

        public void Send(string address, RoutingType? routingType, Message message, CancellationToken cancellationToken)
        {
            CheckAddress(address);
            CheckMessage(message);

            SendInternal(address, routingType, message);
        }

        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        private static void CheckAddress(string address)
        {
            if (string.IsNullOrWhiteSpace(address)) throw new ArgumentNullException(nameof(address), "The address cannot be empty.");
        }

        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        private static void CheckMessage(Message message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message), "The message cannot be null.");
        }
    }
}