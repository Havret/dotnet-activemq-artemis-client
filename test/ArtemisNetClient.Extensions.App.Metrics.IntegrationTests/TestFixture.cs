using System;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Extensions.DependencyInjection;
using ActiveMQ.Artemis.Client.Extensions.Hosting;
using ActiveMQ.Artemis.Client.TestUtils;
using ActiveMQ.Artemis.Client.TestUtils.Logging;
using App.Metrics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace ActiveMQ.Artemis.Client.Extensions.App.Metrics.IntegrationTests
{
    public class TestFixture : IAsyncDisposable
    {
        private readonly IHost _host;
        private readonly CancellationTokenSource _cts;

        private TestFixture(IHost host, IConnection connection, CancellationTokenSource cts)
        {
            _host = host;
            Connection = connection;
            _cts = cts;
        }

        public static async Task<TestFixture> CreateAsync(ITestOutputHelper testOutputHelper, Action<IActiveMqBuilder> configureActiveMq = null, Action<IServiceCollection> configureServices = null)
        {
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5000));
            var endpoints = new[] { EndpointUtil.GetEndpoint() };
            var host = new HostBuilder()
                       .ConfigureWebHost(webBuilder =>
                       {
                           webBuilder
                               .ConfigureServices(services =>
                               {
                                   services.AddMetrics();
                                   services.AddSingleton<IServer>(serviceProvider => new TestServer(serviceProvider));
                                   configureServices?.Invoke(services);
                                   configureActiveMq?.Invoke(services.AddActiveMq( "my-test-artemis", endpoints).ConfigureConnectionFactory((provider, factory) =>
                                   {
                                       factory.LoggerFactory = provider.GetService<ILoggerFactory>();
                                   }));
                                   services.AddActiveMqHostedService();
                               })
                               .Configure(_ => { })
                               .ConfigureLogging((hostingContext, logging) =>
                               {
                                   logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                                   logging.AddProvider(new XUnitLoggerProvider(testOutputHelper));
                                   logging.SetMinimumLevel(LogLevel.Trace);
                               })
                               .ConfigureAppConfiguration((_, config) =>
                               {
                                   config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                               });
                       })
                       .Build();
            await host.StartAsync(cts.Token);

            var connectionFactory = new ConnectionFactory();
            var connection = await connectionFactory.CreateAsync(endpoints, cts.Token);

            return new TestFixture(host, connection, cts);
        }

        public IServiceProvider Services => _host.Services;
        public IConnection Connection { get; }
        public IMetrics Metrics => _host.Services.GetService<IMetrics>();
        public CancellationToken CancellationToken => _cts.Token;

        public async ValueTask DisposeAsync()
        {
            await Connection.DisposeAsync();
            await _host.StopAsync(_cts.Token);
            
            if (_host is IAsyncDisposable host)
                await host.DisposeAsync();
            else
                _host.Dispose();
            
            _cts.Dispose();
        }
    }
}