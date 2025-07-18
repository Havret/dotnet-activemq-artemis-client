using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ActiveMQ.Artemis.Client.Extensions.DependencyInjection;

namespace ActiveMQ.Artemis.Client.Extensions.HealthCheck
{
    /// <summary>
    /// Extension methods for adding ActiveMQ Artemis health checks to the application.
    /// </summary>
    public static class ActiveMqHealthCheckExtensions
    {
        /// <summary>
        /// Adds ActiveMQ Artemis health checks to the specified <see cref="IHealthChecksBuilder"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IHealthChecksBuilder"/> to add the health check to.</param>
        /// <param name="name">The name of the ActiveMQ connection to check.</param>
        /// <param name="factory">Optional factory function to create custom connection for health check.</param>
        /// <returns>The <see cref="IHealthChecksBuilder"/> for further configuration.</returns>
        public static IHealthChecksBuilder AddActiveMqHealthCheck(this IHealthChecksBuilder builder, string name, string[] tags = null)
        {
            // Register the health check factory in the DI container
            builder.Services.AddSingleton(sp => new ArtemisHealthCheckService());

            // Register the health check using the typed approach
            return builder.AddTypeActivatedCheck<ArtemisHealthCheckService>(name,
                failureStatus: HealthStatus.Unhealthy,
                tags: tags ?? new[] { "activemq" });
        }

        public static void ConfigureConnection(IServiceProvider _, IConnection connection)
        {
            connection.ConnectionClosed += (sender, args) => ArtemisHealthCheckService.ConnectionClosed(sender, args);
            connection.ConnectionRecovered += (sender, args) => ArtemisHealthCheckService.ConnectionRecovered(sender, args);
            connection.ConnectionRecoveryError += (sender, args) => ArtemisHealthCheckService.ConnectionRecoveryError(sender, args);
        }        
    }   
    
}
