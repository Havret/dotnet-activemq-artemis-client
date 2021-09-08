using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.AutoRecovering;
using ActiveMQ.Artemis.Client.AutoRecovering.RecoveryPolicy;
using ActiveMQ.Artemis.Client.Builders;
using ActiveMQ.Artemis.Client.Exceptions;
using ActiveMQ.Artemis.Client.MessageIdPolicy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ActiveMQ.Artemis.Client
{
    public class ConnectionFactory
    {
        private IRecoveryPolicy _recoveryPolicy;
        private Func<IMessageIdPolicy> _messageIdPolicyFactory;

        public async Task<IConnection> CreateAsync(IEnumerable<Endpoint> endpoints, CancellationToken cancellationToken)
        {
            var endpointsList = endpoints.ToList();

            if (!endpointsList.Any())
            {
                throw new CreateConnectionException("No endpoints provided.");
            }

            if (AutomaticRecoveryEnabled)
            {
                var autoRecoveringConnection = new AutoRecoveringConnection(LoggerFactory, endpointsList, RecoveryPolicy, MessageIdPolicyFactory);
                await autoRecoveringConnection.InitAsync(cancellationToken).ConfigureAwait(false);
                return autoRecoveringConnection;
            }
            else
            {
                var connectionBuilder = new ConnectionBuilder(LoggerFactory, MessageIdPolicyFactory);
                return await connectionBuilder.CreateAsync(endpointsList.First(), cancellationToken).ConfigureAwait(false);
            }
        }

        public ILoggerFactory LoggerFactory { get; set; } = new NullLoggerFactory();
        public bool AutomaticRecoveryEnabled { get; set; } = true;
        public IRecoveryPolicy RecoveryPolicy
        {
            get => _recoveryPolicy ?? RecoveryPolicyFactory.Default();
            set => _recoveryPolicy = value ?? throw new ArgumentNullException(nameof(value), "Recovery policy cannot be null.");
        }

        public Func<IMessageIdPolicy> MessageIdPolicyFactory
        {
            get => _messageIdPolicyFactory ?? ActiveMQ.Artemis.Client.MessageIdPolicy.MessageIdPolicyFactory.DisableMessageIdPolicy;
            set => _messageIdPolicyFactory = value ?? throw new ArgumentNullException(nameof(value), "MessageId Policy Factory cannot be null.");
        }
    }
}