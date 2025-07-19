using System;
using System.Globalization;
using ActiveMQ.Artemis.Client.AutoRecovering.RecoveryPolicy;
using ActiveMQ.Artemis.Client.MessageIdPolicy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ActiveMQ.Artemis.Client.Extensions.DependencyInjection;
using ActiveMQ.Artemis.Client.Extensions.Hosting;
using ActiveMQ.Artemis.Client.Extensions.HealthCheck;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using ActiveMQ.Artemis.Client.Extensions.HealthCheck;

namespace ActiveMQ.Artemis.Client.Examples.AspNetCore
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddActiveMq(name: "my-artemis-cluster", endpoints: new[] { Endpoint.Create(host: "localhost", port: 61616, "artemis", "artemis"), Endpoint.Create(host: "localhost", port: 5673, "artemis", "artemis") })
                    .ConfigureConnectionFactory((provider, factory) =>
                    {
                        factory.LoggerFactory = provider.GetService<ILoggerFactory>();
                        factory.RecoveryPolicy = RecoveryPolicyFactory.ExponentialBackoff(initialDelay: TimeSpan.FromSeconds(1), maxDelay: TimeSpan.FromSeconds(30), retryCount: 5);
                        factory.MessageIdPolicyFactory = MessageIdPolicyFactory.GuidMessageIdPolicy;
                        factory.AutomaticRecoveryEnabled = true;
                        factory.TCP.KeepAliveTime = 1000 * 30; // 30 seconds
                        factory.TCP.KeepAliveInterval = 1000; // 1 seconds 
                    })
                    .ConfigureConnection((_, connection) =>
                    {
                        connection.ConnectionClosed += (_, args) =>
                        {
                            Console.WriteLine($"Connection closed: ClosedByPeer={args.ClosedByPeer}, Error={args.Error}");
                            //ArtemisHealthCheckService.ConnectionClosed(_, args, $"Connection closed: ClosedByPeer={args.ClosedByPeer}, Error={args.Error}");
                        };
                        connection.ConnectionRecovered += (_, args) =>
                        {
                            Console.WriteLine($"Connection recovered: Endpoint={args.Endpoint}");
                            //ArtemisHealthCheckService.ConnectionRecovered(_, args, $"Connection recovered: Endpoint={args.Endpoint}");
                        };
                        connection.ConnectionRecoveryError += (_, args) =>
                        {
                            Console.WriteLine($"Connection recovered error: Exception={args.Exception}");
                            //ArtemisHealthCheckService.ConnectionRecoveryError(_, args, $"Connection recovered error: Exception={args.Exception}");
                        };
                    })
                    //.ConfigureConnection(ActiveMqHealthCheckExtensions.ConfigureConnection) Optionally configure health check connection events
                    .AddConsumer("a1", RoutingType.Multicast, "q1", async (message, consumer, token, serviceProvider) =>
                    {
                        Console.WriteLine("q1: " + message.GetBody<string>());
                        await consumer.AcceptAsync(message);
                    })
                    .AddConsumer("a1", RoutingType.Multicast, "q2", async (message, consumer, token, serviceProvider) =>
                    {
                        Console.WriteLine("q2: " + message.GetBody<string>());
                        await consumer.AcceptAsync(message);
                    })
                    .AddProducer<MyTypedMessageProducer>("a1", RoutingType.Multicast)
                    .EnableQueueDeclaration()
                    .EnableAddressDeclaration()
                    .EnableHealthChecks(); // Enable health check integration

            services.AddActiveMqHostedService();

            services
             .AddHealthChecks()
             .AddActiveMq("my-artemis-cluster", tags: new[] { "activemq" });
            // .AddActiveMqHealthCheck("my-artemis-cluster"); // Remove or comment out if extension method is not available
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseHealthChecks("/health", new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("activemq")
            });

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", async context =>
                {
                    var messageProducer = context.RequestServices.GetRequiredService<MyTypedMessageProducer>();
                    await messageProducer.SendTextAsync(DateTime.UtcNow.ToString(CultureInfo.InvariantCulture));    
                    await context.Response.WriteAsync("Hello World!");
                });
            });
        }
    }
}