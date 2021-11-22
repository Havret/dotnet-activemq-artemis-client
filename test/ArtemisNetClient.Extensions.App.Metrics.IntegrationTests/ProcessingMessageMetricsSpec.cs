using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Extensions.DependencyInjection;
using App.Metrics;
using Xunit;
using Xunit.Abstractions;

namespace ActiveMQ.Artemis.Client.Extensions.App.Metrics.IntegrationTests
{
    public class ProcessingMessageMetricsSpec
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public ProcessingMessageMetricsSpec(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public async Task Should_record_MessageProcessingTime_metric_when_queue_specified()
        {
            var address = Guid.NewGuid().ToString();
            var queue = Guid.NewGuid().ToString();
            var fixture = await TestFixture.CreateAsync(_testOutputHelper, builder =>
            {
                builder
                    .AddConsumer(address, RoutingType.Multicast, queue, async (message, consumer, _, _) =>
                    {
                        await consumer.AcceptAsync(message);
                    })
                    .EnableAddressDeclaration()
                    .EnableQueueDeclaration()
                    .AddMetrics(options => options.Context = "active-mq-metrics");
            });

            var producer = await fixture.Connection.CreateProducerAsync(address, RoutingType.Multicast, fixture.CancellationToken);
            await producer.SendAsync(new Message("foo"), fixture.CancellationToken);

            MetricsContextValueSource metricsForContext = null;
            SpinWait.SpinUntil(() =>
            {
                metricsForContext = fixture.Metrics.Snapshot.GetForContext("active-mq-metrics");
                return metricsForContext.Histograms.Any();
            }, TimeSpan.FromSeconds(5));

            var metric = metricsForContext.Histograms.FirstOrDefault(x => x.Name.Contains("Message Processing Time"));
            Assert.NotNull(metric);
            Assert.Equal("ns", metric.Unit.Name);
            var tags = (IDictionary<string, string>) metric.Tags.ToDictionary();
            Assert.Equal(address, Assert.Contains("Address", tags));
            Assert.Equal(RoutingType.Multicast.ToString(), Assert.Contains("RoutingType", tags));
            Assert.Equal(queue, Assert.Contains("Queue", tags));
        }
        
        [Fact]
        public async Task Should_record_MessageProcessingTime_metric_when_queue_not_specified()
        {
            var address = Guid.NewGuid().ToString();
            var fixture = await TestFixture.CreateAsync(_testOutputHelper, builder =>
            {
                builder
                    .AddConsumer(address, RoutingType.Anycast, async (message, consumer, _, _) =>
                    {
                        await consumer.AcceptAsync(message);
                    })
                    .AddMetrics(options => options.Context = "active-mq-metrics");
            });

            var producer = await fixture.Connection.CreateProducerAsync(address, fixture.CancellationToken);
            await producer.SendAsync(new Message("foo"), fixture.CancellationToken);

            MetricsContextValueSource metricsForContext = null;
            SpinWait.SpinUntil(() =>
            {
                metricsForContext = fixture.Metrics.Snapshot.GetForContext("active-mq-metrics");
                return metricsForContext.Histograms.Any();
            }, TimeSpan.FromSeconds(5));

            var metric = metricsForContext.Histograms.FirstOrDefault(x => x.Name.Contains("Message Processing Time"));
            Assert.NotNull(metric);
            Assert.Equal("ns", metric.Unit.Name);
            var tags = (IDictionary<string, string>) metric.Tags.ToDictionary();
            Assert.Equal(address, Assert.Contains("Address", tags));
            Assert.Equal(RoutingType.Anycast.ToString(), Assert.Contains("RoutingType", tags));
            Assert.DoesNotContain("Queue", tags);
        }
        
        [Fact]
        public async Task Should_record_MessageProcessingRate_metric_when_queue_specified()
        {
            var address = Guid.NewGuid().ToString();
            var queue = Guid.NewGuid().ToString();
            var fixture = await TestFixture.CreateAsync(_testOutputHelper, builder =>
            {
                builder
                    .AddConsumer(address, RoutingType.Multicast, queue, async (message, consumer, _, _) =>
                    {
                        await consumer.AcceptAsync(message);
                    })
                    .EnableAddressDeclaration()
                    .EnableQueueDeclaration()
                    .AddMetrics(options => options.Context = "active-mq-metrics");
            });

            var producer = await fixture.Connection.CreateProducerAsync(address, RoutingType.Multicast, fixture.CancellationToken);
            await producer.SendAsync(new Message("foo"), fixture.CancellationToken);

            MetricsContextValueSource metricsForContext = null;
            SpinWait.SpinUntil(() =>
            {
                metricsForContext = fixture.Metrics.Snapshot.GetForContext("active-mq-metrics");
                return metricsForContext.Meters.Any();
            }, TimeSpan.FromSeconds(5));

            var metric = metricsForContext.Meters.FirstOrDefault(x => x.Name.Contains("Message Processing Rate"));
            Assert.NotNull(metric);
            Assert.Equal("msg", metric.Unit.Name);
            var tags = (IDictionary<string, string>) metric.Tags.ToDictionary();
            Assert.Equal(address, Assert.Contains("Address", tags));
            Assert.Equal(RoutingType.Multicast.ToString(), Assert.Contains("RoutingType", tags));
            Assert.Equal(queue, Assert.Contains("Queue", tags));
        }
        
        [Fact]
        public async Task Should_record_MessageProcessingRate_metric_when_queue_not_specified()
        {
            var address = Guid.NewGuid().ToString();
            var fixture = await TestFixture.CreateAsync(_testOutputHelper, builder =>
            {
                builder
                    .AddConsumer(address, RoutingType.Anycast, async (message, consumer, _, _) =>
                    {
                        await consumer.AcceptAsync(message);
                    })
                    .AddMetrics(options => options.Context = "active-mq-metrics");
            });

            var producer = await fixture.Connection.CreateProducerAsync(address, fixture.CancellationToken);
            await producer.SendAsync(new Message("foo"), fixture.CancellationToken);

            MetricsContextValueSource metricsForContext = null;
            SpinWait.SpinUntil(() =>
            {
                metricsForContext = fixture.Metrics.Snapshot.GetForContext("active-mq-metrics");
                return metricsForContext.Meters.Any();
            }, TimeSpan.FromSeconds(5));

            var metric = metricsForContext.Meters.FirstOrDefault(x => x.Name.Contains("Message Processing Rate"));
            Assert.NotNull(metric);
            Assert.Equal("msg", metric.Unit.Name);
            var tags = (IDictionary<string, string>) metric.Tags.ToDictionary();
            Assert.Equal(address, Assert.Contains("Address", tags));
            Assert.Equal(RoutingType.Anycast.ToString(), Assert.Contains("RoutingType", tags));
            Assert.DoesNotContain("Queue", tags);
        }
    }
}