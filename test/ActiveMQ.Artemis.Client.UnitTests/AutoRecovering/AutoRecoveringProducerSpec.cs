using System;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.AutoRecovering.RecoveryPolicy;
using ActiveMQ.Artemis.Client.Exceptions;
using ActiveMQ.Artemis.Client.InternalUtilities;
using ActiveMQ.Artemis.Client.UnitTests.Utils;
using Amqp.Framing;
using Amqp.Handler;
using Xunit;
using Xunit.Abstractions;

namespace ActiveMQ.Artemis.Client.UnitTests.AutoRecovering
{
    public class AutoRecoveringProducerSpec : ActiveMQNetSpec
    {
        public AutoRecoveringProducerSpec(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task Should_be_able_to_SendAsync_message_when_connection_restored()
        {
            var (producer, messageProcessor, host, connection) = await CreateReattachedProducer();
            await producer.SendAsync(new Message("foo"));

            var message = messageProcessor.Dequeue(Timeout);
            Assert.NotNull(message);
            Assert.Equal("foo", message.GetBody<string>());

            await DisposeUtil.DisposeAll(producer, connection, host);
        }

        [Fact]
        public async Task Should_be_able_to_Send_message_when_connection_restored()
        {
            var (producer, messageProcessor, host, connection) = await CreateReattachedProducer();

            producer.Send(new Message("foo"));

            var message = messageProcessor.Dequeue(Timeout);
            Assert.NotNull(message);
            Assert.Equal("foo", message.GetBody<string>());
            
            await DisposeUtil.DisposeAll(producer, connection, host);
        }

        [Fact]
        public async Task Should_be_able_to_SendAsync_message_when_SendAsync_invoked_before_connection_restored()
        {
            var endpoint = GetUniqueEndpoint();

            var host1 = CreateOpenedContainerHost(endpoint);

            var connection = await CreateConnection(endpoint);
            var producer = await connection.CreateProducerAsync("a1", RoutingType.Anycast);

            await DisposeHostAndWaitUntilConnectionNotified(host1, connection);

            var cts = new CancellationTokenSource(Timeout);
            var produceTask = producer.SendAsync(new Message("foo"), cts.Token);

            var host2 = CreateOpenedContainerHost(endpoint);
            var messageProcessor = host2.CreateMessageProcessor("a1");

            await produceTask;

            var message = messageProcessor.Dequeue(Timeout);
            Assert.NotNull(message);
            Assert.Equal("foo", message.GetBody<string>());

            await DisposeUtil.DisposeAll(connection, host2);
        }

        [Fact]
        public async Task Throws_when_recovery_policy_gave_up_and_producer_was_not_able_to_SendAsync_message()
        {
            var endpoint = GetUniqueEndpoint();

            var host1 = CreateOpenedContainerHost(endpoint);

            var connectionFactory = CreateConnectionFactory();
            connectionFactory.RecoveryPolicy = RecoveryPolicyFactory.ConstantBackoff(TimeSpan.FromMilliseconds(100), 1);

            await using var connection = await connectionFactory.CreateAsync(endpoint);

            var producer = await connection.CreateProducerAsync("a1", RoutingType.Anycast);

            await DisposeHostAndWaitUntilConnectionNotified(host1, connection);

            var cts = new CancellationTokenSource(Timeout);
            await Assert.ThrowsAsync<ProducerClosedException>(() => producer.SendAsync(new Message("foo"), cts.Token));
        }
        
        [Fact]
        public async Task Throws_when_recovery_policy_gave_up_and_producer_was_not_able_to_Send_message()
        {
            var endpoint = GetUniqueEndpoint();

            var host1 = CreateOpenedContainerHost(endpoint);

            var connectionFactory = CreateConnectionFactory();
            connectionFactory.RecoveryPolicy = RecoveryPolicyFactory.ConstantBackoff(TimeSpan.FromMilliseconds(100), 1);

            await using var connection = await connectionFactory.CreateAsync(endpoint);

            var producer = await connection.CreateProducerAsync("a1", RoutingType.Anycast);

            await DisposeHostAndWaitUntilConnectionNotified(host1, connection);

            var cts = new CancellationTokenSource(Timeout);
            Assert.Throws<ProducerClosedException>(() => producer.Send(new Message("foo"), cts.Token));
        }

        [Fact]
        public async Task Should_be_able_to_Send_message_when_Send_invoked_before_connection_restored()
        {
            var endpoint = GetUniqueEndpoint();

            var host1 = CreateOpenedContainerHost(endpoint);

            var connectionClosed = new ManualResetEvent(false);
            var connection = await CreateConnection(endpoint);
            connection.ConnectionClosed += (_, _) => connectionClosed.Set();
            
            var producer = await connection.CreateProducerAsync("a1", RoutingType.Anycast);

            host1.Dispose();
            
            // make sure that the connection was closed, as we don't want to send a message to host1 
            Assert.True(connectionClosed.WaitOne(Timeout));

            // run on another thread as we don't want to block here 
            var produceTask = Task.Run(() => producer.Send(new Message("foo")));

            var host2 = CreateContainerHost(endpoint);
            var messageProcessor = host2.CreateMessageProcessor("a1");
            host2.Open();

            await produceTask;

            var message = messageProcessor.Dequeue(Timeout);
            Assert.NotNull(message);
            Assert.Equal("foo", message.GetBody<string>());

            await DisposeUtil.DisposeAll(connection, host2);
        }

        [Fact]
        public async Task Should_recreate_producers_on_connection_recovery()
        {
            var endpoint = GetUniqueEndpoint();
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

            var host1 = CreateOpenedContainerHost(endpoint, testHandler);

            var connection = await CreateConnection(endpoint);
            await connection.CreateProducerAsync("a1", RoutingType.Anycast);
            await connection.CreateProducerAsync("a2", RoutingType.Anycast);

            Assert.True(producersAttached.Wait(Timeout));
            producersAttached.Reset();

            host1.Dispose();

            var host2 = CreateOpenedContainerHost(endpoint, testHandler);

            Assert.True(producersAttached.Wait(Timeout));

            await DisposeUtil.DisposeAll(connection, host2);
        }

        [Fact]
        public async Task Should_not_recreate_disposed_producers()
        {
            var endpoint = GetUniqueEndpoint();
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

            var host1 = CreateOpenedContainerHost(endpoint, testHandler);

            var connection = await CreateConnection(endpoint);
            var producer = await connection.CreateProducerAsync("a1");

            Assert.True(producerAttached.WaitOne(Timeout));
            await producer.DisposeAsync();

            producerAttached.Reset();
            host1.Dispose();

            using var host2 = CreateOpenedContainerHost(endpoint, testHandler);

            Assert.False(producerAttached.WaitOne(ShortTimeout));
        }

        private async Task<(IProducer producer, MessageProcessor messageProcessor, TestContainerHost host, IConnection connection)> CreateReattachedProducer()
        {
            var endpoint = GetUniqueEndpoint();
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

            var host1 = CreateOpenedContainerHost(endpoint, testHandler);

            var connection = await CreateConnection(endpoint);
            var producer = await connection.CreateProducerAsync("a1", RoutingType.Anycast);
            Assert.NotNull(producer);

            Assert.True(producerAttached.WaitOne(Timeout), "Producer failed to attach within specified timeout.");
            producerAttached.Reset();

            host1.Dispose();

            var host2 = CreateContainerHost(endpoint, testHandler);
            var messageProcessor = host2.CreateMessageProcessor("a1");
            host2.Open();

            // wait until sender link is reattached
            Assert.True(producerAttached.WaitOne(Timeout), "Producer failed to reattach within specified timeout.");
            return (producer, messageProcessor, host2, connection);
        }
    }
}