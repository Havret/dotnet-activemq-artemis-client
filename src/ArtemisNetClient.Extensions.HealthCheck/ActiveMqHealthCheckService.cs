using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ActiveMQ.Artemis.Client.Extensions.HealthCheck
{

    public class ArtemisHealthCheckService : IHealthCheck
    {
        private static bool IsOpen = true;
        private static string Description = "Connection is open";       

        public static void ConnectionRecovered(object? sender, ConnectionRecoveredEventArgs e, string description = "Connection recovered")
        {
            IsOpen = true;
            Description = description;
        }

        public static void ConnectionRecoveryError(object? sender, ConnectionRecoveryErrorEventArgs e, string description = "Connection recovery failed")
        {
            IsOpen = false;
            Description = description;
        }

        public static void ConnectionClosed(object? sender, ConnectionClosedEventArgs e, string description = "Connection closed")
        {
            IsOpen = false;
            Description = description;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            HealthCheckResult result = new(IsOpen ? HealthStatus.Healthy : HealthStatus.Unhealthy, description: Description);
            return await Task.FromResult(result);
        }
    }
}