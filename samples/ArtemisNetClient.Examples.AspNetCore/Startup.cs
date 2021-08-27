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

namespace ActiveMQ.Artemis.Client.Examples.AspNetCore
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
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
                    .EnableAddressDeclaration();

            services.AddActiveMqHostedService();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", async context =>
                {
                    var messageProducer = context.RequestServices.GetService<MyTypedMessageProducer>();
                    await messageProducer.SendTextAsync(DateTime.UtcNow.ToString(CultureInfo.InvariantCulture));
                    await context.Response.WriteAsync("Hello World!");
                });
            });
        }
    }
}