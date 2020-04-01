using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Net.AutoRecovering;
using ActiveMQ.Net.AutoRecovering.RecoveryPolicy;
using ActiveMQ.Net.Builders;
using ActiveMQ.Net.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ActiveMQ.Net
{
    public class ConnectionFactory
    {
        private IRecoveryPolicy _recoveryPolicy;

        public async Task<IConnection> CreateAsync(IEnumerable<Endpoint> endpoints, CancellationToken cancellationToken)
        {
            var endpointsList = endpoints.ToList();

            if (!endpointsList.Any())
            {
                throw new CreateConnectionException("No endpoints provided.");
            }

            if (AutomaticRecoveryEnabled)
            {
                var autoRecoveringConnection = new AutoRecoveringConnection(LoggerFactory, endpointsList, RecoveryPolicy);
                await autoRecoveringConnection.InitAsync(cancellationToken).ConfigureAwait(false);
                return autoRecoveringConnection;
            }
            else
            {
                var connectionBuilder = new ConnectionBuilder(LoggerFactory);
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
    }
}