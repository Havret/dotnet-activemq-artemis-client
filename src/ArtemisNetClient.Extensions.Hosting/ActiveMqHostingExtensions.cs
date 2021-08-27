using Microsoft.Extensions.DependencyInjection;

namespace ActiveMQ.Artemis.Client.Extensions.Hosting
{
    public static class ActiveMqHostingExtensions
    {
        public static IServiceCollection AddActiveMqHostedService(this IServiceCollection services)
        {
            return services.AddHostedService<ActiveMqHostedService>();
        }
    }
}