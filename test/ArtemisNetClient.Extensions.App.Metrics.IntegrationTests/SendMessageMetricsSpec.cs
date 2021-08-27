using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Extensions.DependencyInjection;
using App.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace ActiveMQ.Artemis.Client.Extensions.App.Metrics.IntegrationTests
{
    public class SendMessageMetricsSpec
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public SendMessageMetricsSpec(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public async Task Should_record_MessageSendTime_metric_when_address_RoutingType_specified()
        {
            var address = Guid.NewGuid().ToString();
            var fixture = await TestFixture.CreateAsync(_testOutputHelper, builder =>
            {
                builder
                    .AddProducer<TestProducer>(address, RoutingType.Multicast)
                    .AddMetrics(options => { options.Context = "active-mq-metrics"; });
            });

            var producer = fixture.Services.GetService<TestProducer>();
            await producer.Producer.SendAsync(new Message("foo"), fixture.CancellationToken);

            var metricsForContext = fixture.Metrics.Snapshot.GetForContext("active-mq-metrics");
            var metric = metricsForContext.Histograms.FirstOrDefault(x => x.Name.Contains("Message Send Time"));
            Assert.NotNull(metric);
            Assert.Equal("ns", metric.Unit.Name);
            var tags = (IDictionary<string, string>) metric.Tags.ToDictionary();
            Assert.Equal(address, Assert.Contains("Address", tags));
            Assert.Equal(RoutingType.Multicast.ToString(), Assert.Contains("RoutingType", tags));
        }
        
        [Fact]
        public async Task Should_record_MessageSendTime_metric_when_address_RoutingType_not_specified()
        {
            var address = Guid.NewGuid().ToString();
            var fixture = await TestFixture.CreateAsync(_testOutputHelper, builder =>
            {
                builder
                    .AddProducer<TestProducer>(address)
                    .AddMetrics(options => { options.Context = "active-mq-metrics"; });
            });

            var producer = fixture.Services.GetService<TestProducer>();
            await producer.Producer.SendAsync(new Message("foo"), fixture.CancellationToken);

            var metricsForContext = fixture.Metrics.Snapshot.GetForContext("active-mq-metrics");
            var metric = metricsForContext.Histograms.FirstOrDefault(x => x.Name.Contains("Message Send Time"));
            Assert.NotNull(metric);
            Assert.Equal("ns", metric.Unit.Name);
            var tags = (IDictionary<string, string>) metric.Tags.ToDictionary();
            Assert.Equal(address, Assert.Contains("Address", tags));
            Assert.DoesNotContain("RoutingType", tags);
        }
        
        [Fact]
        public async Task Should_record_MessageSendRate_metric_when_address_RoutingType_specified()
        {
            var address = Guid.NewGuid().ToString();
            var fixture = await TestFixture.CreateAsync(_testOutputHelper, builder =>
            {
                builder
                    .AddProducer<TestProducer>(address, RoutingType.Multicast)
                    .AddMetrics(options => { options.Context = "active-mq-metrics"; });
            });

            var producer = fixture.Services.GetService<TestProducer>();
            await producer.Producer.SendAsync(new Message("foo"), fixture.CancellationToken);

            var metricsForContext = fixture.Metrics.Snapshot.GetForContext("active-mq-metrics");
            var metric = metricsForContext.Meters.FirstOrDefault(x => x.Name.Contains("Message Send Rate"));
            Assert.NotNull(metric);
            Assert.Equal("msg", metric.Unit.Name);
            var tags = (IDictionary<string, string>) metric.Tags.ToDictionary();
            Assert.Equal(address, Assert.Contains("Address", tags));
            Assert.Equal(RoutingType.Multicast.ToString(), Assert.Contains("RoutingType", tags));
        }
        
        [Fact]
        public async Task Should_record_MessageSendRate_metric_when_address_RoutingType_not_specified()
        {
            var address = Guid.NewGuid().ToString();
            var fixture = await TestFixture.CreateAsync(_testOutputHelper, builder =>
            {
                builder
                    .AddProducer<TestProducer>(address)
                    .AddMetrics(options => { options.Context = "active-mq-metrics"; });
            });

            var producer = fixture.Services.GetService<TestProducer>();
            await producer.Producer.SendAsync(new Message("foo"), fixture.CancellationToken);

            var metricsForContext = fixture.Metrics.Snapshot.GetForContext("active-mq-metrics");
            var metric = metricsForContext.Meters.FirstOrDefault(x => x.Name.Contains("Message Send Rate"));
            Assert.NotNull(metric);
            Assert.Equal("msg", metric.Unit.Name);
            var tags = (IDictionary<string, string>) metric.Tags.ToDictionary();
            Assert.Equal(address, Assert.Contains("Address", tags));
            Assert.DoesNotContain("RoutingType", tags);
        }

        private class TestProducer
        {
            public IProducer Producer { get; }
            public TestProducer(IProducer producer) => Producer = producer;
        }
    }
}