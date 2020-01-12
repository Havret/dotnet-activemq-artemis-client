using System;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Net.Tests.Utils;
using Amqp.Framing;
using Amqp.Handler;
using Xunit;
using Xunit.Abstractions;

namespace ActiveMQ.Net.Tests.AutoRecovering
{
    public class AutoRecoveringConnectionSpec : ActiveMQNetSpec
    {
        public AutoRecoveringConnectionSpec(ITestOutputHelper output) : base(output)
        {
        }
        
        [Fact]
        public async Task Should_reconnect_when_broker_is_available_after_outage_is_over()
        {
            var address = GetUniqueAddress();
            var connectionOpened = new ManualResetEvent(false);

            var testHandler = new TestHandler(@event =>
            {
                switch (@event.Id)
                {
                    case EventId.ConnectionRemoteOpen:
                        connectionOpened.Set();
                        break;
                }
            });

            var host1 = CreateOpenedContainerHost(address, testHandler);

            var connection = await CreateConnection(address);
            Assert.NotNull(connection);
            Assert.True(connectionOpened.WaitOne(Timeout));

            host1.Dispose();

            connectionOpened.Reset();
            using var host2 = CreateOpenedContainerHost(address, testHandler);

            Assert.True(connectionOpened.WaitOne(Timeout));
        }

        [Fact]
        public async Task Should_not_try_to_reconnect_when_connection_explicitly_closed()
        {
            var address = GetUniqueAddress();
            var connectionOpened = new ManualResetEvent(false);

            var testHandler = new TestHandler(@event =>
            {
                switch (@event.Id)
                {
                    case EventId.ConnectionRemoteOpen:
                        connectionOpened.Set();
                        break;
                }
            });

            using var host = CreateOpenedContainerHost(address, testHandler);

            var connection = await CreateConnection(address);
            Assert.NotNull(connection);
            Assert.True(connectionOpened.WaitOne(Timeout));

            connectionOpened.Reset();
            await connection.DisposeAsync();

            Assert.False(connectionOpened.WaitOne(ShortTimeout));
        }
        
        [Fact]
        public async Task Should_recreate_producers_on_connection_recovery()
        {
            var address = GetUniqueAddress();
            var producersAttached = new CountdownEvent(2);
            var testHandler = new TestHandler(@event =>
            {
                switch (@event.Id)
                {
                    case EventId.LinkRemoteOpen when @event.Context is Attach attach && attach.Role:
                        producersAttached.Signal();
                        break;
                }
            });

            var host1 = CreateOpenedContainerHost(address, testHandler);

            var connection = await CreateConnection(address);
            await connection.CreateProducer("a1");
            await connection.CreateProducer("a2");

            Assert.True(producersAttached.Wait(Timeout));
            producersAttached.Reset();

            host1.Dispose();

            using var host2 = CreateOpenedContainerHost(address, testHandler);

            Assert.True(producersAttached.Wait(Timeout));
        }

        [Fact]
        public async Task Should_not_recreate_disposed_producers()
        {
            var address = GetUniqueAddress();
            var producerAttached = new ManualResetEvent(false);
            var testHandler = new TestHandler(@event =>
            {
                switch (@event.Id)
                {
                    case EventId.LinkRemoteOpen when @event.Context is Attach attach && attach.Role:
                        producerAttached.Set();
                        break;
                }
            });

            var host1 = CreateOpenedContainerHost(address, testHandler);

            var connection = await CreateConnection(address);
            var producer = await connection.CreateProducer("a1");

            Assert.True(producerAttached.WaitOne(Timeout));
            await producer.DisposeAsync();
            
            producerAttached.Reset();
            host1.Dispose();

            using var host2 = CreateOpenedContainerHost(address, testHandler);

            Assert.False(producerAttached.WaitOne(ShortTimeout));
        }

        [Fact]
        public async Task Should_recreate_consumers_on_connection_recovery()
        {
            var address = GetUniqueAddress();
            var consumersAttached = new CountdownEvent(2);
            var testHandler = new TestHandler(@event =>
            {
                switch (@event.Id)
                {
                    case EventId.LinkRemoteOpen when @event.Context is Attach attach && !attach.Role:
                        consumersAttached.Signal();
                        break;
                }
            });

            var host1 = CreateOpenedContainerHost(address, testHandler);

            var connection = await CreateConnection(address);
            await connection.CreateConsumerAsync("a1");
            await connection.CreateConsumerAsync("a1");

            Assert.True(consumersAttached.Wait(Timeout));
            consumersAttached.Reset();

            host1.Dispose();

            using var host2 = CreateOpenedContainerHost(address, testHandler);

            Assert.True(consumersAttached.Wait(Timeout));
        }
        
        [Fact]
        public async Task Should_not_recreate_disposed_consumers()
        {
            var address = GetUniqueAddress();
            var consumerAttached = new ManualResetEvent(false);
            var testHandler = new TestHandler(@event =>
            {
                switch (@event.Id)
                {
                    case EventId.LinkRemoteOpen when @event.Context is Attach attach && !attach.Role:
                        consumerAttached.Set();
                        break;
                }
            });

            var host1 = CreateOpenedContainerHost(address, testHandler);

            var connection = await CreateConnection(address);
            var consumer = await connection.CreateConsumerAsync("a1");

            Assert.True(consumerAttached.WaitOne(Timeout));
            await consumer.DisposeAsync();
            
            consumerAttached.Reset();
            host1.Dispose();

            using var host2 = CreateOpenedContainerHost(address, testHandler);

            Assert.False(consumerAttached.WaitOne(ShortTimeout));
        }
    }
}