using System.Threading;
using ActiveMQ.Artemis.Client.Extensions.DependencyInjection;
using App.Metrics;
using App.Metrics.Histogram;
using App.Metrics.Meter;

namespace ActiveMQ.Artemis.Client.Extensions.App.Metrics
{
    internal class ActiveMqMetricsRecorder : ISendObserver, IReceiveObserver
    {
        private readonly IMetrics _metrics;
        private readonly HistogramOptions _messageSendTime;
        private readonly MeterOptions _messageSendRate;
        private readonly HistogramOptions _messageProcessingTime;
        private readonly MeterOptions _messageProcessingRate;
        private readonly AsyncLocal<long> _sendStart = new AsyncLocal<long>();
        private readonly AsyncLocal<long> _processingStart = new AsyncLocal<long>();

        public ActiveMqMetricsRecorder(string name, ActiveMqMetricsOptions options, IMetrics metrics)
        {
            Name = name;
            _metrics = metrics;

            _messageSendTime = new HistogramOptions
            {
                Context = options.Context,
                Name = "Message Send Time",
                MeasurementUnit = Unit.Custom("ns")
            };
            _messageSendRate = new MeterOptions
            {
                Context = options.Context,
                Name = "Message Send Rate",
                MeasurementUnit = Unit.Custom("msg")
            };
            _messageProcessingTime = new HistogramOptions
            {
                Context = options.Context,
                Name = "Message Processing Time",
                MeasurementUnit = Unit.Custom("ns")
            };
            _messageProcessingRate = new MeterOptions
            {
                Context = options.Context,
                Name = "Message Processing Rate",
                MeasurementUnit = Unit.Custom("msg"),
            };
        }

        public string Name { get; }

        public void PreSend(string address, RoutingType? routingType, Message message)
        {
            _sendStart.Value = _metrics.Clock.Nanoseconds;
        }

        public void PostSend(string address, RoutingType? routingType, Message message)
        {
            var elapsed = _metrics.Clock.Nanoseconds - _sendStart.Value;
            var metricTags = GetMetricTags(address, routingType);
            _metrics.Measure.Histogram.Update(_messageSendTime, metricTags, elapsed);
            _metrics.Measure.Meter.Mark(_messageSendRate, metricTags);
        }

        public void PreReceive(string address, RoutingType routingType, string queue, Message message)
        {
            _processingStart.Value = _metrics.Clock.Nanoseconds;
        }

        public void PostReceive(string address, RoutingType routingType, string queue, Message message)
        {
            var elapsed = _metrics.Clock.Nanoseconds - _processingStart.Value;
            var metricTags = GetMetricTags(address, routingType, queue);
            _metrics.Measure.Histogram.Update(_messageProcessingTime, metricTags, elapsed);
            _metrics.Measure.Meter.Mark(_messageProcessingRate, metricTags);
        }

        private static MetricTags GetMetricTags(string address, RoutingType? routingType, string queue = null)
        {
            var metricTags = new MetricTags("Address", address);
            if (routingType != null)
            {
                metricTags = MetricTags.Concat(metricTags, new MetricTags($"{nameof(RoutingType)}", routingType.ToString()));
            }
            if (!string.IsNullOrWhiteSpace(queue))
            {
                metricTags = MetricTags.Concat(metricTags, new MetricTags("Queue", queue));
            }
            return metricTags;
        }
    }
}