using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ActiveMQ.Artemis.Client.Extensions.HealthCheck
{

    /// <summary>
    /// Service for managing ActiveMQ Artemis health checks.
    /// </summary>
    public class ArtemisHealthCheckService : IHealthCheck
    {
        private bool IsOpen = false;
        private string Description = "Connection not established";


        /// <summary>
        /// Marks the connection as open and updates the description accordingly.
        /// </summary>
        public void ConnectionOpen()
        {
            IsOpen = true;
            Description = "Connection is open";
        }

        /// <summary>
        /// Marks the connection as recovered and updates the description accordingly.
        /// </summary>
        public void ConnectionRecovered(object? sender, ConnectionRecoveredEventArgs e, string description = "Connection recovered")
        {
            IsOpen = true;
            Description = description;
        }

        /// <summary>
        /// Marks the connection as closed and updates the description accordingly.
        /// </summary>
        public void ConnectionRecoveryError(object? sender, ConnectionRecoveryErrorEventArgs e, string description = "Connection recovery failed")
        {
            IsOpen = false;
            Description = description;
        }

        /// <summary>
        /// Marks the connection as closed and updates the description accordingly.
        /// </summary>
        public void ConnectionClosed(object? sender, ConnectionClosedEventArgs e, string description = "Connection closed")
        {
            IsOpen = false;
            Description = description;
        }

        /// <summary>
        /// Checks the health of the ActiveMQ connection.
        /// </summary>
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            HealthCheckResult result = new(IsOpen ? HealthStatus.Healthy : HealthStatus.Unhealthy, description: Description);
            return await Task.FromResult(result);
        }
    }
}