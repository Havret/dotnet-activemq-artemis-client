using System;
using ActiveMQ.Artemis.Client.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace ActiveMQ.Artemis.Client.Extensions.LeaderElection
{
    public static class ActiveMqLeaderElectionExtensions
    {
        public static IActiveMqBuilder AddLeaderElection(this IActiveMqBuilder builder, LeaderElectionOptions options = null)
        {
            options ??= new LeaderElectionOptions();
            if (string.IsNullOrWhiteSpace(options.ElectionAddress))
            {
                throw new ArgumentException($"{nameof(LeaderElectionOptions.ElectionAddress)} must be provided.", nameof(LeaderElectionOptions.ElectionAddress));
            }

            if (options.ElectionMessageInterval <= TimeSpan.Zero)
            {
                throw new ArgumentException($"{nameof(LeaderElectionOptions.ElectionMessageInterval)} must greater than 0.", nameof(LeaderElectionOptions.ElectionAddress));
            }
            if (options.HandOverAfterMissedElectionMessages <= 0)
            {
                throw new ArgumentException($"{nameof(LeaderElectionOptions.HandOverAfterMissedElectionMessages)} must greater than 0.", nameof(LeaderElectionOptions.ElectionAddress));
            }
            
            builder.AddConsumer(options.ElectionAddress, RoutingType.Anycast, async (message, consumer, serviceProvider, cancellationToken) =>
            {
                var leaderElection = serviceProvider.GetService<LeaderElection>();
                leaderElection.OnElectionMessage();
                await consumer.AcceptAsync(message).ConfigureAwait(false);
            });
            builder.AddProducer<ElectionMessageProducer>(options.ElectionAddress, RoutingType.Anycast, ServiceLifetime.Singleton);
            builder.Services.AddSingleton(provider => ActivatorUtilities.CreateInstance<LeaderElection>(provider, options));
            builder.Services.AddSingleton<ILeaderElection>(provider => provider.GetRequiredService<LeaderElection>());
            return builder;
        }
    }
}