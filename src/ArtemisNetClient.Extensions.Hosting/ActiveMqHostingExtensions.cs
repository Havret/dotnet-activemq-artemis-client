using Microsoft.Extensions.DependencyInjection;

namespace ActiveMQ.Artemis.Client.Extensions.Hosting
{
    public static class ActiveMqHostingExtensions
    {
        public static IServiceCollection AddActiveMqHostedService(this IServiceCollection services)
        {
            return services.AddHostedService<ActiveMqHostedService>();
        }

        public static IServiceCollection AddActiveMqHostedService(this IServiceCollection services, ActiveMqHostedServiceOptions hostedServiceOptions)
        {
            return hostedServiceOptions.WaitUntilStarted
                ? services.AddHostedService<ActiveMqHostedService>()
                : services.AddHostedService<ActiveMqBackgroundService>();
        }
    }
}