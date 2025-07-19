using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ActiveMQ.Artemis.Client.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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
        /// <param name="connectionName">The name of the ActiveMQ connection configured with AddActiveMq.</param>
        /// <param name="tags">Optional tags for the health check.</param>
        /// <returns>The <see cref="IHealthChecksBuilder"/> for further configuration.</returns>
        public static IHealthChecksBuilder AddActiveMq(this IHealthChecksBuilder builder,
            string name,       
            string[] tags = null)
        { 
            // Register the health check service
            builder.Services.AddKeyedSingleton<ArtemisHealthCheckService>(name);

            return builder.Add(new HealthCheckRegistration(
                name,
                provider => provider.GetRequiredKeyedService<ArtemisHealthCheckService>(name),
                HealthStatus.Unhealthy,
                tags: tags ?? new[] { "activemq" }));         
        }

        /// <summary>
        /// Configures an ActiveMQ connection to work with health checks by attaching the necessary event handlers.
        /// This method should be called on the IActiveMqBuilder returned by AddActiveMq.
        /// </summary>
        /// <param name="builder">The <see cref="IActiveMqBuilder"/> to configure.</param>
        /// <returns>The <see cref="IActiveMqBuilder"/> for further configuration.</returns>
        public static IActiveMqBuilder EnableHealthChecks(this IActiveMqBuilder builder)
        {
            return builder.ConfigureConnection((provider, connection) =>
            {
                var healthCheckService = provider.GetRequiredKeyedService<ArtemisHealthCheckService>(builder.Name);

                healthCheckService.ConnectionOpen();

                // Attach event handlers to the connection for health check updates

                connection.ConnectionClosed += (sender, args) => healthCheckService.ConnectionClosed(sender, args);
                connection.ConnectionRecovered += (sender, args) => healthCheckService.ConnectionRecovered(sender, args);
                connection.ConnectionRecoveryError += (sender, args) => healthCheckService.ConnectionRecoveryError(sender, args);
            });
        }

    }

}
