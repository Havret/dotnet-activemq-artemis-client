using System;
using System.Linq;
using ActiveMQ.Artemis.Client.Extensions.DependencyInjection;
using App.Metrics;
using Microsoft.Extensions.DependencyInjection;

namespace ActiveMQ.Artemis.Client.Extensions.App.Metrics
{
    public static class ActiveMqArtemisClientAppMetricsExtensions
    {
        public static IActiveMqBuilder AddMetrics(this IActiveMqBuilder builder, Action<ActiveMqMetricsOptions> configureOptions)
        {
            builder.Services.AddSingleton(provider =>
            {
                var activeMqMetricsOptions = new ActiveMqMetricsOptions();
                configureOptions.Invoke(activeMqMetricsOptions);
                var metrics = provider.GetRequiredService<IMetrics>();
                return new ActiveMqMetricsRecorder(builder.Name, activeMqMetricsOptions, metrics);
            });
            return builder.AddSendObserver<ActiveMqMetricsRecorder>(provider => provider.GetMetricsRecorder(builder.Name))
                          .AddReceiveObserver<ActiveMqMetricsRecorder>(provider => provider.GetMetricsRecorder(builder.Name));
        }

        private static ActiveMqMetricsRecorder GetMetricsRecorder(this IServiceProvider provider, string name)
        {
            return provider.GetServices<ActiveMqMetricsRecorder>().Single(x => x.Name == name);
        }
    }
}