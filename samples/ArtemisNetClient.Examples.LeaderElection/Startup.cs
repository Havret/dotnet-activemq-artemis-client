using System;
using ActiveMQ.Artemis.Client.AutoRecovering.RecoveryPolicy;
using ActiveMQ.Artemis.Client.Extensions.DependencyInjection;
using ActiveMQ.Artemis.Client.Extensions.Hosting;
using ActiveMQ.Artemis.Client.Extensions.LeaderElection;
using ActiveMQ.Artemis.Client.MessageIdPolicy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ActiveMQ.Artemis.Client.Examples.LeaderElection
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddActiveMq(name: "my-artemis-cluster", endpoints: new[] { Endpoint.Create(host: "localhost", port: 5672, "guest", "guest") })
                    .ConfigureConnectionFactory((provider, factory) =>
                    {
                        factory.LoggerFactory = provider.GetService<ILoggerFactory>();
                        factory.RecoveryPolicy = RecoveryPolicyFactory.ExponentialBackoff(initialDelay: TimeSpan.FromSeconds(1), maxDelay: TimeSpan.FromSeconds(30));
                        factory.MessageIdPolicyFactory = MessageIdPolicyFactory.GuidMessageIdPolicy;
                        factory.AutomaticRecoveryEnabled = true;
                    })
                    .AddLeaderElection(new LeaderElectionOptions
                    {
                        ElectionAddress = "ElectionAddress",
                        ElectionMessageInterval = TimeSpan.FromSeconds(1),
                        HandOverAfterMissedElectionMessages = 3
                    })
                    .EnableQueueDeclaration()
                    .EnableAddressDeclaration();

            services.AddActiveMqHostedService();
            services.AddHostedService<LeaderElectionHostedService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
        }
    }
}