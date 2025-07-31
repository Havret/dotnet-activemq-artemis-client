using ActiveMQ.Artemis.Client.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ActiveMQ.Artemis.Client.Extensions.HealthChecks
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
        /// <param name="activeMqBuilder"></param>
        /// <param name="connectionName">The name of the ActiveMQ connection configured with AddActiveMq.</param>
        /// <param name="tags">Optional tags for the health check.</param>
        /// <returns>The <see cref="IHealthChecksBuilder"/> for further configuration.</returns>
        public static IHealthChecksBuilder AddActiveMq(this IHealthChecksBuilder builder,
            string name,  
            IActiveMqBuilder activeMqBuilder,     
            string[] tags = null)
        { 
            // Register the health check service
            builder.Services.AddKeyedSingleton<ArtemisHealthCheckService>(activeMqBuilder.Name);

            activeMqBuilder.ConfigureConnection((provider, connection) =>
            {
                var healthCheckService = provider.GetRequiredKeyedService<ArtemisHealthCheckService>(activeMqBuilder.Name);

                healthCheckService.ConnectionOpen();

                // Attach event handlers to the connection for health check updates

                connection.ConnectionClosed += (sender, args) => healthCheckService.ConnectionClosed(sender, args);
                connection.ConnectionRecovered += (sender, args) => healthCheckService.ConnectionRecovered(sender, args);
                connection.ConnectionRecoveryError += (sender, args) => healthCheckService.ConnectionRecoveryError(sender, args);
            });

            return builder.Add(new HealthCheckRegistration(
                name,
                provider => provider.GetRequiredKeyedService<ArtemisHealthCheckService>(activeMqBuilder.Name),
                HealthStatus.Unhealthy,
                tags: tags ?? new[] { "activemq" }));         
        }        

    }

}
