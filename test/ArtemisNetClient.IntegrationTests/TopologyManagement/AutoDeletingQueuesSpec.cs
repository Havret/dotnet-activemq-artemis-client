using System;
using System.Linq;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.TestUtils;
using Xunit;
using Xunit.Abstractions;

namespace ActiveMQ.Artemis.Client.IntegrationTests.TopologyManagement
{
    public class AutoDeletingQueuesSpec : ActiveMQNetIntegrationSpec
    {
        public AutoDeletingQueuesSpec(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task Should_auto_delete_queue_when_no_consumers_attached()
        {
            await using var connection = await CreateConnection();
            await using var topologyManager = await connection.CreateTopologyManagerAsync();

            var address = Guid.NewGuid().ToString();
            var queueName = Guid.NewGuid().ToString();
            await topologyManager.CreateQueueAsync(new QueueConfiguration
            {
                Name = queueName,
                RoutingType = RoutingType.Multicast,
                Address = address,
                Durable = true,
                AutoCreateAddress = true,
                AutoDelete = true,
            }, CancellationToken);

            await using var producer = await connection.CreateProducerAsync(address);
            var consumer = await connection.CreateConsumerAsync(address, queueName);

            await producer.SendAsync(new Message("foo"));
            var msg = await consumer.ReceiveAsync();
            await consumer.AcceptAsync(msg);
            await consumer.DisposeAsync();
            
            var queues = await Retry.RetryUntil(
                () => topologyManager.GetQueueNamesAsync(),
                x => !x.Contains(queueName),
                TimeSpan.FromMinutes(1));
            Assert.DoesNotContain(queueName, queues);
        }

        [Fact]
        public async Task Should_delete_queue_after_specified_delay()
        {
            await using var connection = await CreateConnection();
            await using var topologyManager = await connection.CreateTopologyManagerAsync();

            var address = Guid.NewGuid().ToString();
            var queueName = Guid.NewGuid().ToString();
            await topologyManager.CreateQueueAsync(new QueueConfiguration
            {
                Name = queueName,
                RoutingType = RoutingType.Multicast,
                Address = address,
                Durable = true,
                AutoCreateAddress = true,
                AutoDelete = true,
                AutoDeleteDelay = TimeSpan.FromMilliseconds(500)
            }, CancellationToken);

            await using var producer = await connection.CreateProducerAsync(address);
            var consumer = await connection.CreateConsumerAsync(address, queueName);

            await producer.SendAsync(new Message("foo"));
            var msg = await consumer.ReceiveAsync();
            await consumer.AcceptAsync(msg);
            await consumer.DisposeAsync();
            
            var queues = await Retry.RetryUntil(
                () => topologyManager.GetQueueNamesAsync(),
                x => !x.Contains(queueName),
                TimeSpan.FromMinutes(1));
            Assert.DoesNotContain(queueName, queues);
        }

        [Fact]
        public async Task Should_not_delete_queue_when_AutoDeleteMessageCount_not_reached()
        {
            await using var connection = await CreateConnection();
            await using var topologyManager = await connection.CreateTopologyManagerAsync();

            var address = Guid.NewGuid().ToString();
            var queueName = Guid.NewGuid().ToString();
            await topologyManager.CreateQueueAsync(new QueueConfiguration
            {
                Name = queueName,
                RoutingType = RoutingType.Multicast,
                Address = address,
                Durable = true,
                AutoCreateAddress = true,
                AutoDelete = true,
                AutoDeleteMessageCount = 1
            }, CancellationToken);

            await using var producer = await connection.CreateProducerAsync(address);
            var consumer = await connection.CreateConsumerAsync(address, queueName);

            await producer.SendAsync(new Message("foo"));
            await producer.SendAsync(new Message("foo1"));
            await producer.SendAsync(new Message("foo2"));
            
            var msg = await consumer.ReceiveAsync();
            await consumer.AcceptAsync(msg);
            await consumer.DisposeAsync();
            
            Assert.Contains(queueName, await topologyManager.GetQueueNamesAsync());
            
            consumer = await connection.CreateConsumerAsync(address, queueName);
            msg = await consumer.ReceiveAsync();
            await consumer.AcceptAsync(msg);
            await consumer.DisposeAsync();

            // one unacknowledged message remained, thus broker should remove the queue
            var queues = await Retry.RetryUntil(
                () => topologyManager.GetQueueNamesAsync(),
                x => !x.Contains(queueName),
                TimeSpan.FromMinutes(1));
            Assert.DoesNotContain(queueName, queues);
        }
    }
}