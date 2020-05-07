using System;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Net.Transactions;
using Amqp;
using Microsoft.Extensions.Logging;

namespace ActiveMQ.Net
{
    internal class AnonymousProducer : ProducerBase, IAnonymousProducer
    {
        public AnonymousProducer(ILoggerFactory loggerFactory, SenderLink senderLink, TransactionsManager transactionsManager, AnonymousProducerConfiguration configuration) :
            base(loggerFactory, senderLink, transactionsManager, configuration)
        {
        }

        public Task SendAsync(string address, AddressRoutingType routingType, Message message, Transaction transaction, CancellationToken cancellationToken = default)
        {
            CheckAddress(address);
            CheckMessage(message);

            return SendInternalAsync(address, routingType, message, transaction, cancellationToken);
        }

        public void Send(string address, AddressRoutingType routingType, Message message)
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