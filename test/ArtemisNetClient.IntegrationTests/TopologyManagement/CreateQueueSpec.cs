using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace ActiveMQ.Artemis.Client.IntegrationTests.TopologyManagement
{
    public class CreateQueueSpec : ActiveMQNetIntegrationSpec
    {
        public CreateQueueSpec(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task Should_create_queue()
        {
            await using var connection = await CreateConnection();
            await using var topologyManager = await connection.CreateTopologyManagerAsync();

            var address = Guid.NewGuid().ToString();
            await topologyManager.CreateAddressAsync(address, RoutingType.Multicast, CancellationToken);

            var queueName = Guid.NewGuid().ToString();
            await topologyManager.CreateQueueAsync(new QueueConfiguration
            {
                Name = queueName,
                RoutingType = RoutingType.Multicast,
                Address = address,
                Durable = true,
                Exclusive = false,
                GroupRebalance = false,
                GroupBuckets = 64,
                MaxConsumers = -1,
                AutoCreateAddress = false,
                PurgeOnNoConsumers = false
            }, CancellationToken);
        }

        [Fact]
        public async Task Should_create_queue_and_address()
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
                AutoCreateAddress = true
            }, CancellationToken);
        }

        [Fact]
        public async Task Throws_when_queue_already_exists()
        {
            await using var connection = await CreateConnection();
            await using var topologyManager = await connection.CreateTopologyManagerAsync();

            var queueConfiguration = new QueueConfiguration
            {
                Name = Guid.NewGuid().ToString(),
                RoutingType = RoutingType.Multicast,
                Address = Guid.NewGuid().ToString(),
                AutoCreateAddress = true
            };

            await topologyManager.CreateQueueAsync(queueConfiguration, CancellationToken);

            await Assert.ThrowsAsync<InvalidOperationException>(async () => await topologyManager.CreateQueueAsync(queueConfiguration, CancellationToken));
        }

        [Fact]
        public async Task Throws_when_address_does_not_exist_and_AutoCreateAddress_set_to_false()
        {
            await using var connection = await CreateConnection();
            await using var topologyManager = await connection.CreateTopologyManagerAsync();

            var queueConfiguration = new QueueConfiguration
            {
                Name = Guid.NewGuid().ToString(),
                RoutingType = RoutingType.Multicast,
                Address = Guid.NewGuid().ToString(),
                AutoCreateAddress = false
            };

            await Assert.ThrowsAsync<InvalidOperationException>(async () => await topologyManager.CreateQueueAsync(queueConfiguration, CancellationToken));
        }

        [Fact]
        public async Task Should_create_queue_with_filter_expression()
        {
            await using var connection = await CreateConnection();
            await using var topologyManager = await connection.CreateTopologyManagerAsync();

            var address = Guid.NewGuid().ToString();
            var queue = Guid.NewGuid().ToString();
            await topologyManager.CreateQueueAsync(new QueueConfiguration
            {
                Address = address,
                RoutingType = RoutingType.Multicast,
                Name = queue,
                AutoCreateAddress = true,
                FilterExpression = "AMQPriority = 9"
            }, CancellationToken);

            var consumer = await connection.CreateConsumerAsync(address, queue, CancellationToken);
            var producer = await connection.CreateProducerAsync(address, RoutingType.Multicast);

            await producer.SendAsync(new Message("foo1") { Priority = 4 });
            await producer.SendAsync(new Message("foo2") { Priority = 9 });
            
            Assert.Equal("foo2", (await consumer.ReceiveAsync(CancellationToken)).GetBody<string>());
        }

        [Fact]
        public async Task Should_create_queue_with_PurgeOnNoConsumers_flag()
        {
            var connection = await CreateConnection();
            var address = Guid.NewGuid().ToString();
            var queue = Guid.NewGuid().ToString();

            var topologyManager = await connection.CreateTopologyManagerAsync();
            await topologyManager.CreateAddressAsync(address, RoutingType.Multicast);
            await topologyManager.CreateQueueAsync(new QueueConfiguration
            {
                Address = address,
                Name = queue,
                RoutingType = RoutingType.Multicast,
                MaxConsumers = -1,
                PurgeOnNoConsumers = true
            });

            var producer = await connection.CreateProducerAsync(address, RoutingType.Multicast);
            var consumer = await connection.CreateConsumerAsync(address, queue);

            await producer.SendAsync(new Message("foo1"));

            Assert.Equal("foo1", (await consumer.ReceiveAsync(CancellationToken)).GetBody<string>());

            await connection.DisposeAsync();
            
            await using var newConnection = await CreateConnection();
            await using var newQueue1Consumer = await newConnection.CreateConsumerAsync(address, queue);
            await using var newProducer = await newConnection.CreateProducerAsync(address, RoutingType.Multicast);

            await newProducer.SendAsync(new Message("foo2"));

            Assert.Equal("foo2", (await newQueue1Consumer.ReceiveAsync(CancellationToken)).GetBody<string>());
        }

        [Fact]
        public async Task Should_create_last_value_queue()
        {
            var connection = await CreateConnection();
            var address = Guid.NewGuid().ToString();
            var queue = Guid.NewGuid().ToString();
            var lastValueKey = "my-last-value-key";

            var topologyManager = await connection.CreateTopologyManagerAsync();
            await topologyManager.CreateAddressAsync(address, RoutingType.Multicast);
            await topologyManager.CreateQueueAsync(new QueueConfiguration
            {
                Address = address,
                Name = queue,
                RoutingType = RoutingType.Multicast,
                LastValueKey = lastValueKey
            });
            
            var producer = await connection.CreateProducerAsync(address, RoutingType.Multicast);
            
            // Send series of messages with the same last value key
            await producer.SendAsync(new Message("foo1") { ApplicationProperties = { [lastValueKey] = "1" } });
            await producer.SendAsync(new Message("foo2") { ApplicationProperties = { [lastValueKey] = "1" } });
            await producer.SendAsync(new Message("foo3") { ApplicationProperties = { [lastValueKey] = "1" } });
            
            // Send series of messages with another last value key
            await producer.SendAsync(new Message("foo4") { ApplicationProperties = { [lastValueKey] = "2" } });
            await producer.SendAsync(new Message("foo5") { ApplicationProperties = { [lastValueKey] = "2" } });
            await producer.SendAsync(new Message("foo6") { ApplicationProperties = { [lastValueKey] = "2" } });
            
            var consumer = await connection.CreateConsumerAsync(address, queue, CancellationToken);
            
            // Only messages foo3 and foo6 should survive
            Assert.Equal("foo3", (await consumer.ReceiveAsync(CancellationToken)).GetBody<string>());
            Assert.Equal("foo6", (await consumer.ReceiveAsync(CancellationToken)).GetBody<string>());
        }

        [Fact]
        public async Task Should_create_non_destructive_queue()
        {
            var connection = await CreateConnection();
            var address = Guid.NewGuid().ToString();
            var queue = Guid.NewGuid().ToString();

            var topologyManager = await connection.CreateTopologyManagerAsync();
            await topologyManager.CreateAddressAsync(address, RoutingType.Multicast);
            await topologyManager.CreateQueueAsync(new QueueConfiguration
            {
                Address = address,
                Name = queue,
                RoutingType = RoutingType.Multicast,
                NonDestructive = true
            });
            
            var producer = await connection.CreateProducerAsync(address, RoutingType.Multicast);

            await producer.SendAsync(new Message("foo"));

            var consumer = await connection.CreateConsumerAsync(address, queue, CancellationToken);
            var msg = await consumer.ReceiveAsync(CancellationToken);
            await consumer.AcceptAsync(msg);
            Assert.Equal("foo", msg.GetBody<string>());

            // close consumer
            await consumer.DisposeAsync();
            
            // recreate consumer, and the message should be redelivered as the queue was created as non-destructive
            consumer = await connection.CreateConsumerAsync(address, queue, CancellationToken);
            msg = await consumer.ReceiveAsync(CancellationToken);
            await consumer.AcceptAsync(msg);
            Assert.Equal("foo", msg.GetBody<string>());
        }
    }
}