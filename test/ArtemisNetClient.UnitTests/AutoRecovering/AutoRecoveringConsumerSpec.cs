using System;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.AutoRecovering;
using ActiveMQ.Artemis.Client.AutoRecovering.RecoveryPolicy;
using ActiveMQ.Artemis.Client.Exceptions;
using ActiveMQ.Artemis.Client.InternalUtilities;
using ActiveMQ.Artemis.Client.UnitTests.Utils;
using Amqp;
using Amqp.Framing;
using Amqp.Handler;
using Amqp.Listener;
using Microsoft.Extensions.Logging.Abstractions;
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
        public async Task Should_not_suspend_consumer_when_receive_fails_on_consumer_already_replaced()
        {
            using var host = CreateOpenedContainerHost();
            var messageSource = host.CreateMessageSource("a1");
            await using var connection = await CreateConnectionWithoutAutomaticRecovery(host.Endpoint);
            var configuration = new ConsumerConfiguration { Address = "a1", RoutingType = RoutingType.Anycast };
            await using var consumer = new AutoRecoveringConsumer(NullLoggerFactory.Instance, configuration);
            var recoveryRequested = false;
            consumer.RecoveryRequested += () => recoveryRequested = true;
            await consumer.RecoverAsync(connection, CancellationToken);

            using var cts = new CancellationTokenSource(Timeout);
            var receiveTask = consumer.ReceiveAsync(cts.Token).AsTask();

            // Replacing the consumer disposes the old one, which fails the receive pending on it. The failure comes
            // late, the consumer has already been replaced.
            await consumer.RecoverAsync(connection, CancellationToken);

            // The receive is retried on the replacement, which is left as it is
            messageSource.Enqueue(new Message("foo"));
            var message = await receiveTask.WaitAsync(Timeout);
            Assert.Equal("foo", message.GetBody<string>());
            Assert.False(consumer.IsSuspended);
            Assert.False(recoveryRequested);
        }

        [Fact]
        public async Task Should_fail_receive_waiting_for_recovery_when_consumer_disposed()
        {
            var underlyingConsumer = new FakeConsumer();
            var consumer = new AutoRecoveringConsumer(NullLoggerFactory.Instance, new ConsumerConfiguration());
            var recoveryRequested = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            consumer.RecoveryRequested += () => recoveryRequested.TrySetResult(true);
            await consumer.RecoverAsync(new FakeConnection(underlyingConsumer), CancellationToken);

            var receiveTask = consumer.ReceiveAsync(CancellationToken).AsTask();
            await underlyingConsumer.ReceiveCalled.Task.WaitAsync(Timeout);

            // The receive waits for the recovery
            underlyingConsumer.PendingReceive.SetException(new ConsumerClosedException());
            await recoveryRequested.Task.WaitAsync(Timeout);

            await consumer.DisposeAsync();

            // There is not going to be any recovery, the receive must not wait for it
            await Assert.ThrowsAsync<ObjectDisposedException>(() => receiveTask.WaitAsync(Timeout));
        }

        [Fact]
        public async Task Should_not_suspend_consumer_once_disposed()
        {
            var consumer = new AutoRecoveringConsumer(NullLoggerFactory.Instance, new ConsumerConfiguration());
            await consumer.RecoverAsync(new FakeConnection(new FakeConsumer()), CancellationToken);
            await consumer.DisposeAsync();

            // E.g. the connection suspending all its recoverables at the same time
            consumer.Suspend();

            Assert.False(consumer.IsSuspended);
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
            var host1 = CreateOpenedContainerHost();
            var host2 = CreateContainerHost();

            var connection = await CreateConnection(new[] { host1.Endpoint, host2.Endpoint });
            var consumer = await connection.CreateConsumerAsync("a1", RoutingType.Anycast);
            
            var cts = new CancellationTokenSource(Timeout);
            var receiveTask = consumer.ReceiveAsync(cts.Token);

            host2.Open();
            host1.Dispose();

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
            Assert.IsType<Rejected>(dispositionContext.DeliveryState);
            Assert.True(dispositionContext.Settled);
            
            await DisposeUtil.DisposeAll(consumer, connection, host);
        }

        [Fact]
        public async Task Should_be_able_to_modify_messages_when_connection_restored()
        {
            var (consumer, messageSource, host, connection) = await CreateReattachedConsumer();

            messageSource.Enqueue(new Message("foo"));

            var cts = new CancellationTokenSource(Timeout);
            var message = await consumer.ReceiveAsync(cts.Token);
            consumer.Modify(message, deliveryFailed: true, undeliverableHere: false);

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

            await Assert.ThrowsAsync<ConsumerClosedException>(async () => await receiveTask);
        }

        [Fact]
        public async Task Throws_on_receive_when_resource_deleted()
        {
            var host = CreateOpenedContainerHost();
            var linkProcessor = host.CreateTestLinkProcessor();

            ListenerLink listenerLink = null;
            linkProcessor.SetHandler(context =>
            {
                listenerLink = context.Link;
                return false;
            });

            await using var connection = await CreateConnection(host.Endpoint);

            var consumer = await connection.CreateConsumerAsync("a1", RoutingType.Anycast);

            var receiveTask = consumer.ReceiveAsync(CancellationToken);
            
            try
            {
                await listenerLink.CloseAsync(Timeout, new Error(ErrorCode.ResourceDeleted) { Description = "Queue was deleted: a1" });
            }
            catch (Exception)
            {
                // ignored
            }

            await Assert.ThrowsAsync<ConsumerClosedException>(async () => await receiveTask);
        }

        [Fact]
        public async Task Should_not_recreate_consumer_when_resource_deleted()
        {
            var host1 = CreateOpenedContainerHost();
            var host2 = CreateContainerHost();
            var host3 = CreateContainerHost();
            
            ListenerLink listenerLink1 = null;
            host1.CreateTestLinkProcessor().SetHandler(context =>
            {
                listenerLink1 = context.Link;
                return false;
            });

            await using var connection = await CreateConnection(new[] { host1.Endpoint, host2.Endpoint, host3.Endpoint });

            var consumer = await connection.CreateConsumerAsync("a1", RoutingType.Anycast);

            var receiveTask = consumer.ReceiveAsync(CancellationToken);

            try
            {
                await listenerLink1.CloseAsync(Timeout, new Error(ErrorCode.ResourceDeleted) { Description = "Queue was deleted: a1" });
            }
            catch (Exception)
            {
                // ignored
            }

            await Assert.ThrowsAsync<ConsumerClosedException>(async () => await receiveTask);

            await DisposeHostAndWaitUntilConnectionNotified(host1, connection);

            var waitUntilConnectionRecoveredTask = WaitUntilConnectionRecovered(connection);

            host2.CreateTestLinkProcessor().SetHandler(context =>
            {
                context.Complete(new Error(ErrorCode.NotFound) { Description = "Queue: 'a1' does not exist" });
                return true;
            });
            host2.Open();

            // wait until the connection recovered
            await waitUntilConnectionRecoveredTask;
            
            // make sure that the consumer remains closed
            await Assert.ThrowsAsync<ConsumerClosedException>(async () => await consumer.ReceiveAsync(CancellationToken));

            // perform again auto-recovery cycle, to make sure that
            // no further attempts to recover the consumer will be made
            await DisposeHostAndWaitUntilConnectionNotified(host2, connection);
            
            waitUntilConnectionRecoveredTask = WaitUntilConnectionRecovered(connection);
            
            ListenerLink listenerLink3 = null;
            host3.CreateTestLinkProcessor().SetHandler(context =>
            {
                listenerLink3 = context.Link;
                return false;
            });
            host3.Open();
            
            // wait until the connection recovered
            await waitUntilConnectionRecoveredTask;
            
            // make sure that no listener link was created on the final host
            Assert.Null(listenerLink3);
        }

        [Fact]
        public async Task Should_recreate_consumers_on_connection_recovery()
        {
            var endpoint = GetUniqueEndpoint();
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

            var host1 = CreateOpenedContainerHost(endpoint, testHandler);

            var connection = await CreateConnection(endpoint);
            await connection.CreateConsumerAsync("a1", RoutingType.Anycast);
            await connection.CreateConsumerAsync("a1", RoutingType.Anycast);

            Assert.True(consumersAttached.Wait(Timeout));
            consumersAttached.Reset();

            host1.Dispose();

            var host2 = CreateOpenedContainerHost(endpoint, testHandler);

            Assert.True(consumersAttached.Wait(Timeout));

            await DisposeUtil.DisposeAll(connection, host2);
        }

        [Fact]
        public async Task Should_not_recreate_disposed_consumers()
        {
            var endpoint = GetUniqueEndpoint();
            var consumerAttached = new ManualResetEvent(false);
            var connectionRecovered = new ManualResetEvent(false);
            var testHandler = new TestHandler(@event =>
            {
                switch (@event.Id)
                {
                    case EventId.LinkRemoteOpen when @event.Context is Attach attach && !attach.Role:
                        consumerAttached.Set();
                        break;
                }
            });

            var host1 = CreateOpenedContainerHost(endpoint, testHandler);

            var connection = await CreateConnection(endpoint);
            connection.ConnectionRecovered += (_, _) => connectionRecovered.Set();
            var consumer = await connection.CreateConsumerAsync("a1", RoutingType.Anycast);

            Assert.True(consumerAttached.WaitOne(Timeout));
            await consumer.DisposeAsync();

            consumerAttached.Reset();

            await DisposeHostAndWaitUntilConnectionNotified(host1, connection);

            var host2 = CreateOpenedContainerHost(endpoint, testHandler);

            Assert.True(connectionRecovered.WaitOne(Timeout));

            Assert.False(consumerAttached.WaitOne(ShortTimeout));

            await DisposeUtil.DisposeAll(connection, host2);
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