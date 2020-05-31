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
    public class AutoRecoveringConsumerSpec : ActiveMQNetSpec
    {
        public AutoRecoveringConsumerSpec(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task Should_be_able_to_receive_messages_when_connection_restored()
        {
            var (consumer, messageSource, host, connection) = await CreateReattachedConsumer();

            messageSource.Enqueue(new Message("foo"));

            var cts = new CancellationTokenSource(Timeout);
            var message = await consumer.ReceiveAsync(cts.Token);
            Assert.NotNull(message);
            Assert.Equal("foo", message.GetBody<string>());

            await DisposeUtil.DisposeAll(consumer, connection, host);
        }

        [Fact]
        public async Task Should_be_able_to_receive_message_when_connection_restored_after_receive_called()
        {
            var endpoint = GetUniqueEndpoint();
            var host1 = CreateOpenedContainerHost(endpoint);

            var connection = await CreateConnection(endpoint);
            var consumer = await connection.CreateConsumerAsync("a1", RoutingType.Anycast);
            
            var cts = new CancellationTokenSource(Timeout);
            var receiveTask = consumer.ReceiveAsync(cts.Token);

            host1.Dispose();

            var host2 = CreateOpenedContainerHost(endpoint);
            var messageSource = host2.CreateMessageSource("a1");
            messageSource.Enqueue(new Message("foo"));

            var message = await receiveTask;
            Assert.NotNull(message);
            Assert.Equal("foo", message.GetBody<string>());

            await DisposeUtil.DisposeAll(consumer, connection, host2);
        }

        [Fact]
        public async Task Should_be_able_to_accept_messages_when_connection_restored()
        {
            var (consumer, messageSource, host, connection) = await CreateReattachedConsumer();

            messageSource.Enqueue(new Message("foo"));

            var cts = new CancellationTokenSource(Timeout);
            var message = await consumer.ReceiveAsync(cts.Token);
            await consumer.AcceptAsync(message);

            var dispositionContext = messageSource.GetNextDisposition(Timeout);
            Assert.IsType<Accepted>(dispositionContext.DeliveryState);
            Assert.True(dispositionContext.Settled);
            
            await DisposeUtil.DisposeAll(consumer, connection, host);
        }

        [Fact]
        public async Task Should_be_able_to_receive_and_reject_messages_when_connection_restored()
        {
            var (consumer, messageSource, host, connection) = await CreateReattachedConsumer();

            messageSource.Enqueue(new Message("foo"));

            var cts = new CancellationTokenSource(Timeout);
            var message = await consumer.ReceiveAsync(cts.Token);
            consumer.Reject(message);

            var dispositionContext = messageSource.GetNextDisposition(Timeout);
            Assert.IsType<Modified>(dispositionContext.DeliveryState);
            Assert.True(dispositionContext.Settled);
            
            await DisposeUtil.DisposeAll(consumer, connection, host);
        }
        
        [Fact]
        public async Task Throws_when_recovery_policy_gave_up_and_consumer_was_not_able_to_receive_message_if_ReceiveAsync_called_after_connection_lost()
        {
            var endpoint = GetUniqueEndpoint();

            var host = CreateOpenedContainerHost(endpoint);

            var connectionFactory = CreateConnectionFactory();
            connectionFactory.RecoveryPolicy = RecoveryPolicyFactory.ConstantBackoff(TimeSpan.FromMilliseconds(100), 1);

            await using var connection = await connectionFactory.CreateAsync(endpoint);

            var consumer = await connection.CreateConsumerAsync("a1", RoutingType.Anycast);

            await DisposeHostAndWaitUntilConnectionNotified(host, connection);

            var cts = new CancellationTokenSource(Timeout);
            await Assert.ThrowsAsync<ConsumerClosedException>(async() => await consumer.ReceiveAsync(cts.Token));
        }
        
        [Fact]
        public async Task Throws_when_recovery_policy_gave_up_and_consumer_was_not_able_to_receive_message_if_ReceiveAsync_called_before_connection_lost()
        {
            var endpoint = GetUniqueEndpoint();

            var host = CreateOpenedContainerHost(endpoint);

            var connectionFactory = CreateConnectionFactory();
            connectionFactory.RecoveryPolicy = RecoveryPolicyFactory.ConstantBackoff(TimeSpan.FromMilliseconds(100), 1);

            await using var connection = await connectionFactory.CreateAsync(endpoint);

            var consumer = await connection.CreateConsumerAsync("a1", RoutingType.Anycast);
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            var receiveTask = consumer.ReceiveAsync(cts.Token);

            await DisposeHostAndWaitUntilConnectionNotified(host, connection);

            await Assert.ThrowsAsync<ConsumerClosedException>(async() => await receiveTask);
        }

        private async Task<(IConsumer consumer, MessageSource messageSource, TestContainerHost host, IConnection connection)> CreateReattachedConsumer()
        {
            var endpoint = GetUniqueEndpoint();
            var consumerAttached = new ManualResetEvent(false);

            var testHandler = new TestHandler(@event =>
            {
                switch (@event.Id)
                {
                    case EventId.LinkLocalOpen when @event.Context is Attach attach && !attach.Role:
                        consumerAttached.Set();
                        break;
                }
            });

            var host1 = CreateOpenedContainerHost(endpoint, testHandler);

            var connection = await CreateConnection(endpoint);
            var consumer = await connection.CreateConsumerAsync("a1", RoutingType.Anycast);

            Assert.True(consumerAttached.WaitOne(Timeout));
            host1.Dispose();
            consumerAttached.Reset();

            var host2 = CreateContainerHost(endpoint, testHandler);
            var messageSource = host2.CreateMessageSource("a1");
            host2.Open();

            // wait until sender link is reattached
            Assert.True(consumerAttached.WaitOne(Timeout));

            return (consumer, messageSource, host2, connection);
        }
    }
}