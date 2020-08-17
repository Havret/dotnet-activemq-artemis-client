using System;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Exceptions;
using Xunit;
using Xunit.Abstractions;

namespace ActiveMQ.Artemis.Client.IntegrationTests
{
    public class AddressModelSpec : ActiveMQNetIntegrationSpec
    {
        public AddressModelSpec(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task Should_send_and_receive_using_anycast_address()
        {
            await using var connection = await CreateConnection();
            var address = Guid.NewGuid().ToString();
            await using var producer = await connection.CreateProducerAsync(address, RoutingType.Anycast);
            await using var consumer1 = await connection.CreateConsumerAsync(address, RoutingType.Anycast);
            await using var consumer2 = await connection.CreateConsumerAsync(address, RoutingType.Anycast);

            await producer.SendAsync(new Message("foo1"));
            await producer.SendAsync(new Message("foo2"));

            Assert.Equal("foo1", (await consumer1.ReceiveAsync(CancellationToken)).GetBody<string>());
            Assert.Equal("foo2", (await consumer2.ReceiveAsync(CancellationToken)).GetBody<string>());
        }

        [Fact]
        public async Task Should_attach_to_durable_queue_in_simple_anycast_scenario()
        {
            await using var connection = await CreateConnection();
            var address = Guid.NewGuid().ToString();
            await using var producer = await connection.CreateProducerAsync(address, RoutingType.Anycast);
            var consumer = await connection.CreateConsumerAsync(address, RoutingType.Anycast);

            await producer.SendAsync(new Message("foo"));

            Assert.Equal("foo", (await consumer.ReceiveAsync(CancellationToken)).GetBody<string>());

            await consumer.DisposeAsync();

            await using var newConsumer = await connection.CreateConsumerAsync(address, RoutingType.Anycast);
            Assert.Equal("foo", (await newConsumer.ReceiveAsync(CancellationToken)).GetBody<string>());
        }

        [Fact]
        public async Task Should_send_and_receive_using_multicast_address()
        {
            await using var connection = await CreateConnection();
            var address = Guid.NewGuid().ToString();
            await using var producer = await connection.CreateProducerAsync(address, RoutingType.Multicast);
            await using var consumer1 = await connection.CreateConsumerAsync(address, RoutingType.Multicast);
            await using var consumer2 = await connection.CreateConsumerAsync(address, RoutingType.Multicast);

            await producer.SendAsync(new Message("foo"));

            Assert.Equal("foo", (await consumer1.ReceiveAsync(CancellationToken)).GetBody<string>());
            Assert.Equal("foo", (await consumer2.ReceiveAsync(CancellationToken)).GetBody<string>());
        }

        [Fact]
        public async Task Should_attach_to_non_durable_queue_in_simple_multicast_scenario()
        {
            await using var connection = await CreateConnection();
            var address = Guid.NewGuid().ToString();
            await using var producer = await connection.CreateProducerAsync(address, RoutingType.Multicast);
            var consumer = await connection.CreateConsumerAsync(address, RoutingType.Multicast);

            await producer.SendAsync(new Message("foo1"));

            Assert.Equal("foo1", (await consumer.ReceiveAsync(CancellationToken)).GetBody<string>());

            // when the consumer goes down, so as the queue it was attached to
            // the message is then lost
            await consumer.DisposeAsync();

            await using var newConsumer = await connection.CreateConsumerAsync(address, RoutingType.Multicast);

            await producer.SendAsync(new Message("foo2"));

            Assert.Equal("foo2", (await newConsumer.ReceiveAsync(CancellationToken)).GetBody<string>());
        }

        [Fact]
        public async Task Should_receive_messages_using_shared_queue_with_multicast_routing_type()
        {
            await using var connection1 = await CreateConnection();
            await using var connection2 = await CreateConnection();
            var address = Guid.NewGuid().ToString();
            var queue1 = Guid.NewGuid().ToString();
            var queue2 = Guid.NewGuid().ToString();

            var topologyManager = await connection1.CreateTopologyManagerAsync();
            await topologyManager.CreateAddressAsync(address, RoutingType.Multicast);
            await topologyManager.CreateQueueAsync(new QueueConfiguration
            {
                Address = address,
                Name = queue1,
                RoutingType = RoutingType.Multicast,
                MaxConsumers = -1,
                PurgeOnNoConsumers = true
            });
            await topologyManager.CreateQueueAsync(new QueueConfiguration
            {
                Address = address,
                Name = queue2,
                RoutingType = RoutingType.Multicast,
                PurgeOnNoConsumers = true
            });

            await using var producer = await connection1.CreateProducerAsync(address, RoutingType.Multicast);
            var queue1Consumer1 = await connection1.CreateConsumerAsync(address, queue1);
            var queue1Consumer2 = await connection1.CreateConsumerAsync(address, queue1);
            var queue2Consumer1 = await connection1.CreateConsumerAsync(address, queue2);
            var queue2Consumer2 = await connection1.CreateConsumerAsync(address, queue2);

            await producer.SendAsync(new Message("foo1"));
            await producer.SendAsync(new Message("foo2"));

            Assert.Equal("foo1", (await queue1Consumer1.ReceiveAsync(CancellationToken)).GetBody<string>());
            Assert.Equal("foo2", (await queue1Consumer2.ReceiveAsync(CancellationToken)).GetBody<string>());
            Assert.Equal("foo1", (await queue2Consumer1.ReceiveAsync(CancellationToken)).GetBody<string>());
            Assert.Equal("foo2", (await queue2Consumer2.ReceiveAsync(CancellationToken)).GetBody<string>());

            // Make sure that the queues were non durable.
            await queue1Consumer1.DisposeAsync();
            await queue1Consumer2.DisposeAsync();
            await queue2Consumer1.DisposeAsync();
            await queue2Consumer2.DisposeAsync();

            // give broker time to clean up the resources
            await Task.Delay(100);

            await using var newQueue1Consumer1 = await connection2.CreateConsumerAsync(address, queue1);
            await using var newQueue1Consumer2 = await connection2.CreateConsumerAsync(address, queue1);
            await using var newQueue2Consumer1 = await connection2.CreateConsumerAsync(address, queue2);
            await using var newQueue2Consumer2 = await connection2.CreateConsumerAsync(address, queue2);

            await producer.SendAsync(new Message("foo3"));
            await producer.SendAsync(new Message("foo4"));

            Assert.Equal("foo3", (await newQueue1Consumer1.ReceiveAsync(CancellationToken)).GetBody<string>());
            Assert.Equal("foo4", (await newQueue1Consumer2.ReceiveAsync(CancellationToken)).GetBody<string>());
            Assert.Equal("foo3", (await newQueue2Consumer1.ReceiveAsync(CancellationToken)).GetBody<string>());
            Assert.Equal("foo4", (await newQueue2Consumer2.ReceiveAsync(CancellationToken)).GetBody<string>());
        }

        [Fact]
        public async Task Should_receive_messages_using_shared_durable_queue_with_multicast_routing_type()
        {
            await using var connection1 = await CreateConnection();
            await using var connection2 = await CreateConnection();
            var address = Guid.NewGuid().ToString();
            var queue1 = Guid.NewGuid().ToString();
            var queue2 = Guid.NewGuid().ToString();

            var topologyManager = await connection1.CreateTopologyManagerAsync();
            await topologyManager.CreateAddressAsync(address, RoutingType.Multicast);
            await topologyManager.CreateQueueAsync(new QueueConfiguration
            {
                Address = address,
                Name = queue1,
                RoutingType = RoutingType.Multicast,
                MaxConsumers = -1,
                Durable = true
            });
            await topologyManager.CreateQueueAsync(new QueueConfiguration
            {
                Address = address,
                Name = queue2,
                RoutingType = RoutingType.Multicast,
                Durable = true
            });


            await using var producer = await connection1.CreateProducerAsync(address, RoutingType.Multicast);
            var queue1Consumer1 = await connection1.CreateConsumerAsync(address, queue1);
            var queue1Consumer2 = await connection2.CreateConsumerAsync(address, queue1);
            var queue2Consumer1 = await connection1.CreateConsumerAsync(address, queue2);
            var queue2Consumer2 = await connection2.CreateConsumerAsync(address, queue2);

            await producer.SendAsync(new Message("foo1"));
            await producer.SendAsync(new Message("foo2"));

            Assert.Equal("foo1", (await queue1Consumer1.ReceiveAsync(CancellationToken)).GetBody<string>());
            Assert.Equal("foo2", (await queue1Consumer2.ReceiveAsync(CancellationToken)).GetBody<string>());
            Assert.Equal("foo1", (await queue2Consumer1.ReceiveAsync(CancellationToken)).GetBody<string>());
            Assert.Equal("foo2", (await queue2Consumer2.ReceiveAsync(CancellationToken)).GetBody<string>());
            
            await using var newQueue1Consumer = await connection1.CreateConsumerAsync(address, queue1);
            await using var newQueue2Consumer = await connection2.CreateConsumerAsync(address, queue2);

            // make sure that the queues are durable
            await queue1Consumer1.DisposeAsync();
            await queue1Consumer2.DisposeAsync();
            await queue2Consumer1.DisposeAsync();
            await queue2Consumer2.DisposeAsync();

            Assert.Equal("foo1", (await newQueue1Consumer.ReceiveAsync(CancellationToken)).GetBody<string>());
            Assert.Equal("foo2", (await newQueue1Consumer.ReceiveAsync(CancellationToken)).GetBody<string>());
            Assert.Equal("foo1", (await newQueue2Consumer.ReceiveAsync(CancellationToken)).GetBody<string>());
            Assert.Equal("foo2", (await newQueue2Consumer.ReceiveAsync(CancellationToken)).GetBody<string>());
        }

        [Fact]
        public async Task Should_receive_messages_using_non_shared_durable_queue_with_multicast_routing_type()
        {
            await using var connection = await CreateConnection();
            var address = Guid.NewGuid().ToString();
            var queue = Guid.NewGuid().ToString();
            var topologyManager = await connection.CreateTopologyManagerAsync();
            await topologyManager.CreateAddressAsync(address, RoutingType.Multicast);
            await topologyManager.CreateQueueAsync(new QueueConfiguration
            {
                Address = address,
                Name = queue,
                RoutingType = RoutingType.Multicast,
                MaxConsumers = 1,
                Durable = true
            });

            await using var producer = await connection.CreateProducerAsync(address, RoutingType.Multicast);
            var consumer = await connection.CreateConsumerAsync(address, queue);

            await producer.SendAsync(new Message("foo1"));
            await producer.SendAsync(new Message("foo2"));

            Assert.Equal("foo1", (await consumer.ReceiveAsync(CancellationToken)).GetBody<string>());
            Assert.Equal("foo2", (await consumer.ReceiveAsync(CancellationToken)).GetBody<string>());

            // make sure that the queue is durable
            await consumer.DisposeAsync();

            await using var newConsumer = await connection.CreateConsumerAsync(address, queue);

            Assert.Equal("foo1", (await newConsumer.ReceiveAsync(CancellationToken)).GetBody<string>());
            Assert.Equal("foo2", (await newConsumer.ReceiveAsync(CancellationToken)).GetBody<string>());
        }

        [Fact]
        public async Task Throws_on_attempt_to_attach_more_than_one_consumer_to_non_shared_durable_queue_with_multicast_routing_type()
        {
            await using var connection = await CreateConnection();
            var address = Guid.NewGuid().ToString();
            var queue = Guid.NewGuid().ToString();

            var topologyManager = await connection.CreateTopologyManagerAsync();
            await topologyManager.CreateAddressAsync(address, RoutingType.Multicast);
            await topologyManager.CreateQueueAsync(new QueueConfiguration
            {
                Address = address,
                Name = queue,
                RoutingType = RoutingType.Multicast,
                MaxConsumers = 1,
                Durable = true
            });

            await using var consumer = await connection.CreateConsumerAsync(address, queue);

            await Assert.ThrowsAsync<CreateConsumerException>(async () => await connection.CreateConsumerAsync(address, queue));
        }

        [Fact]
        public async Task Should_send_messages_to_anycast_or_multicast_queues_depending_on_producer_routing_type()
        {
            await using var connection = await CreateConnection();
            var address = Guid.NewGuid().ToString();
            var anycastQueue = Guid.NewGuid().ToString();
            var multicastQueue = Guid.NewGuid().ToString();

            var topologyManager = await connection.CreateTopologyManagerAsync();
            await topologyManager.CreateAddressAsync(address, new[] { RoutingType.Anycast, RoutingType.Multicast });
            await topologyManager.CreateQueueAsync(new QueueConfiguration
            {
                Address = address,
                Name = anycastQueue,
                RoutingType = RoutingType.Anycast,
            });
            await topologyManager.CreateQueueAsync(new QueueConfiguration
            {
                Address = address,
                Name = multicastQueue,
                RoutingType = RoutingType.Multicast,
            });


            await using var anycastConsumer = await connection.CreateConsumerAsync(address, anycastQueue);
            await using var multicastConsumer = await connection.CreateConsumerAsync(address, multicastQueue);

            var anycastProducer = await connection.CreateProducerAsync(address, RoutingType.Anycast);
            var multicastProducer = await connection.CreateProducerAsync(address, RoutingType.Multicast);
            var anycastAndMulticastProducer = await connection.CreateProducerAsync(address);

            anycastProducer.Send(new Message("anycast"));
            Assert.Equal("anycast", (await anycastConsumer.ReceiveAsync()).GetBody<string>());
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await multicastConsumer.ReceiveAsync(new CancellationTokenSource(TimeSpan.FromMilliseconds(100)).Token));

            multicastProducer.Send(new Message("multicast"));
            Assert.Equal("multicast", (await multicastConsumer.ReceiveAsync()).GetBody<string>());
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await anycastConsumer.ReceiveAsync(new CancellationTokenSource(TimeSpan.FromMilliseconds(100)).Token));

            anycastAndMulticastProducer.Send(new Message("anycast-multicast"));
            Assert.Equal("anycast-multicast", (await anycastConsumer.ReceiveAsync()).GetBody<string>());
            Assert.Equal("anycast-multicast", (await multicastConsumer.ReceiveAsync()).GetBody<string>());
        }
    }
}