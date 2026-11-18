using System;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.AutoRecovering.RecoveryPolicy;
using ActiveMQ.Artemis.Client.Exceptions;
using ActiveMQ.Artemis.Client.InternalUtilities;
using ActiveMQ.Artemis.Client.UnitTests.Utils;
using Amqp;
using Amqp.Framing;
using Amqp.Handler;
using Amqp.Listener;
using Xunit;
using Xunit.Abstractions;

namespace ActiveMQ.Artemis.Client.UnitTests.AutoRecovering
{
    public class AutoRecoveringConnectionSpec : ActiveMQNetSpec
    {
        public AutoRecoveringConnectionSpec(ITestOutputHelper output) : base(output)
        {
        }
        
        [Fact]
        public async Task Should_reconnect_when_broker_outage_is_over()
        {
            var endpoint = GetUniqueEndpoint();
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

            var host1 = CreateOpenedContainerHost(endpoint, testHandler);

            var connection = await CreateConnection(endpoint);
            Assert.NotNull(connection);
            Assert.True(connectionOpened.WaitOne(Timeout));

            host1.Dispose();

            connectionOpened.Reset();
            var host2 = CreateOpenedContainerHost(endpoint, testHandler);

            Assert.True(connectionOpened.WaitOne(Timeout));

            await DisposeUtil.DisposeAll(connection, host2);
        }

        [Fact]
        public async Task Should_not_try_to_reconnect_when_connection_explicitly_closed()
        {
            var endpoint = GetUniqueEndpoint();
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

            using var host = CreateOpenedContainerHost(endpoint, testHandler);

            await using var connection = await CreateConnection(endpoint);
            Assert.NotNull(connection);
            Assert.True(connectionOpened.WaitOne(Timeout));

            connectionOpened.Reset();
            await connection.DisposeAsync();

            Assert.False(connectionOpened.WaitOne(ShortTimeout));
        }

        [Fact]
        public async Task Should_connect_to_first_endpoint_when_auto_recovering_disabled()
        {
            var (host1, connectedToHost1) = CreateHost();
            var (host2, connectedToHost2) = CreateHost();

            var connectionFactory = CreateConnectionFactory();
            connectionFactory.AutomaticRecoveryEnabled = false;

            var connection = await connectionFactory.CreateAsync(new[] { host1.Endpoint, host2.Endpoint });

            Assert.True(connection.IsOpened);
            Assert.True(connectedToHost1.WaitOne(Timeout));
            Assert.False(connectedToHost2.WaitOne(ShortTimeout));

            await DisposeUtil.DisposeAll(connection, host1, host2);
        }
        
        [Fact]
        public async Task Should_connect_to_the_second_endpoint_when_first_endpoint_disconnected()
        {
            var host1 = CreateContainerHost();
            var (host2, connectedToHost2) = CreateHost();

            var connection = await CreateConnection(new[] { host1.Endpoint, host2.Endpoint });

            Assert.True(connectedToHost2.WaitOne(Timeout));
            Assert.True(connection.IsOpened);
        }

        [Fact]
        public async Task Should_reconnect_to_the_second_endpoint_after_first_endpoint_disconnected()
        {
            var (host1, connectedToHost1) = CreateHost();
            var (host2, connectedToHost2) = CreateHost();

            var connection = await CreateConnection(new[] { host1.Endpoint, host2.Endpoint });

            Assert.True(connection.IsOpened);
            Assert.True(connectedToHost1.WaitOne(Timeout));
            Assert.False(connectedToHost2.WaitOne(ShortTimeout));

            host1.Dispose();
            
            Assert.True(connectedToHost2.WaitOne(Timeout));
            Assert.False(connectedToHost1.WaitOne(ShortTimeout));
            Assert.True(connection.IsOpened);

            await DisposeUtil.DisposeAll(connection, host2);
        }

        [Fact]
        public async Task Should_trigger_ConnectionRecovered_when_connection_recovered()
        {
            var host1 = CreateOpenedContainerHost();
            var host2 = CreateOpenedContainerHost();

            var connection = await CreateConnection(new[] { host1.Endpoint, host2.Endpoint });

            var connectionRecovered = new AutoResetEvent(false);
            Endpoint fallbackEndpoint = null;
            connection.ConnectionRecovered += (_, args) =>
            {
                fallbackEndpoint = args.Endpoint;
                connectionRecovered.Set();
            };

            Assert.True(connection.IsOpened);

            host1.Dispose();
            
            Assert.True(connectionRecovered.WaitOne(Timeout));
            Assert.Equal(host2.Endpoint, fallbackEndpoint);

            await DisposeUtil.DisposeAll(connection, host2);
        }

        [Fact]
        public async Task Should_trigger_ConnectionRecoveryError_when_connection_recovery_failed()
        {
            var host = CreateOpenedContainerHost();

            var connectionFactory = CreateConnectionFactory();
            connectionFactory.RecoveryPolicy = RecoveryPolicyFactory.ConstantBackoff(TimeSpan.FromMilliseconds(10), retryCount: 1);

            await using var connection = await connectionFactory.CreateAsync(host.Endpoint);

            var connectionRecoveryFailed = new AutoResetEvent(false);
            Exception connectionRecoveryError = null;
            connection.ConnectionRecoveryError += (_, args) =>
            {
                connectionRecoveryError = args.Exception;
                connectionRecoveryFailed.Set();
            };

            Assert.True(connection.IsOpened);

            host.Dispose();

            Assert.True(connectionRecoveryFailed.WaitOne(Timeout));
            Assert.NotNull(connectionRecoveryError);
        }

        [Fact]
        public async Task Should_raise_ConnectionClosed_even_if_connection_lost_in_recovery_mode()
        {
            var host1 = CreateOpenedContainerHost();
            using var host2 = CreateOpenedContainerHost();

            await using var connection = await CreateConnection(new[] { host1.Endpoint, host2.Endpoint });

            var connectionRecovered = new AutoResetEvent(false);
            var connectionClosed = new ManualResetEvent(false);
            connection.ConnectionRecovered += (_, _) => connectionRecovered.Set();
            connection.ConnectionClosed += (_, _) => { connectionClosed.Set(); };

            Assert.True(connection.IsOpened);

            host1.Dispose();

            Assert.True(connectionRecovered.WaitOne(Timeout));
            Assert.True(connectionClosed.WaitOne(Timeout));
        }

        [Fact]
        public async Task Should_create_anonymous_producer_once_connection_recovered_when_requested_during_outage()
        {
            var endpoint = GetUniqueEndpoint();
            var host1 = CreateOpenedContainerHost(endpoint);
            await using var connection = await CreateConnection(endpoint);
            await DisposeHostAndWaitUntilConnectionNotified(host1, connection);

            var createProducerTask = connection.CreateAnonymousProducerAsync(CancellationToken);

            using var host2 = CreateOpenedContainerHost(endpoint);
            await using var producer = await createProducerTask;

            // TestLinkEndpoint settles messages on anonymous links, so a completed send proves the link works
            await producer.SendAsync("a1", new Message("foo"), CancellationToken);
        }

        [Fact]
        public async Task Should_create_anonymous_producer_once_connection_recovered_when_connection_lost_during_creation()
        {
            var endpoint = GetUniqueEndpoint();
            var host1 = CreateContainerHostThatWillNeverSendAttachFrameBack(endpoint);
            await using var connection = await CreateConnection(endpoint);

            var createProducerTask = connection.CreateAnonymousProducerAsync(CancellationToken);
            await DisposeHostAndWaitUntilConnectionNotified(host1, connection);

            using var host2 = CreateOpenedContainerHost(endpoint);
            await using var producer = await createProducerTask;

            await producer.SendAsync("a1", new Message("foo"), CancellationToken);
        }

        [Fact]
        public async Task Should_cancel_anonymous_producer_creation_requested_during_outage()
        {
            var host = CreateOpenedContainerHost();
            await using var connection = await CreateConnection(host.Endpoint);
            await DisposeHostAndWaitUntilConnectionNotified(host, connection);

            using var cts = new CancellationTokenSource(ShortTimeout);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => connection.CreateAnonymousProducerAsync(cts.Token));
        }

        [Fact]
        public async Task Throws_on_attempt_to_create_anonymous_producer_when_connection_disposed_during_outage()
        {
            var host = CreateOpenedContainerHost();
            var connection = await CreateConnection(host.Endpoint);
            await DisposeHostAndWaitUntilConnectionNotified(host, connection);

            var createProducerTask = connection.CreateAnonymousProducerAsync(CancellationToken);
            await connection.DisposeAsync();

            await Assert.ThrowsAsync<ObjectDisposedException>(() => createProducerTask);
        }

        [Fact]
        public async Task Throws_on_attempt_to_create_anonymous_producer_when_connection_recovery_failed()
        {
            var host = CreateOpenedContainerHost();
            var connectionFactory = CreateConnectionFactory();
            connectionFactory.RecoveryPolicy = RecoveryPolicyFactory.ConstantBackoff(TimeSpan.FromMilliseconds(500), retryCount: 2);
            await using var connection = await connectionFactory.CreateAsync(host.Endpoint);
            await DisposeHostAndWaitUntilConnectionNotified(host, connection);

            var createProducerTask = connection.CreateAnonymousProducerAsync(CancellationToken);

            var exception = await Assert.ThrowsAsync<ConnectionClosedException>(() => createProducerTask);
            Assert.NotNull(exception.InnerException);
            await Assert.ThrowsAsync<ConnectionClosedException>(() => connection.CreateAnonymousProducerAsync(CancellationToken));
        }

        [Fact]
        public async Task Should_create_producer_once_connection_recovered_when_requested_during_outage()
        {
            var endpoint = GetUniqueEndpoint();
            var host1 = CreateOpenedContainerHost(endpoint);
            await using var connection = await CreateConnection(endpoint);
            await DisposeHostAndWaitUntilConnectionNotified(host1, connection);

            var createProducerTask = connection.CreateProducerAsync("a1", RoutingType.Anycast, CancellationToken);

            using var host2 = CreateOpenedContainerHost(endpoint);
            await using var producer = await createProducerTask;

            await producer.SendAsync(new Message("foo"), CancellationToken);
        }

        [Fact]
        public async Task Should_create_consumer_once_connection_recovered_when_requested_during_outage()
        {
            var endpoint = GetUniqueEndpoint();
            var host1 = CreateOpenedContainerHost(endpoint);
            await using var connection = await CreateConnection(endpoint);
            await DisposeHostAndWaitUntilConnectionNotified(host1, connection);

            var createConsumerTask = connection.CreateConsumerAsync("a1", RoutingType.Anycast, CancellationToken);

            using var host2 = CreateOpenedContainerHost(endpoint);
            await using var consumer = await createConsumerTask;
        }

        [Fact]
        public async Task Should_recover_connection_after_creation_stuck_on_attach_was_cancelled()
        {
            var endpoint = GetUniqueEndpoint();
            var host1 = CreateContainerHostThatWillNeverSendAttachFrameBack(endpoint);
            await using var connection = await CreateConnection(endpoint);
            var connectionRecovered = new AutoResetEvent(false);
            connection.ConnectionRecovered += (_, _) => connectionRecovered.Set();

            using (var cts = new CancellationTokenSource(ShortTimeout))
            {
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => connection.CreateAnonymousProducerAsync(cts.Token));
            }

            await DisposeHostAndWaitUntilConnectionNotified(host1, connection);
            using var host2 = CreateOpenedContainerHost(endpoint);

            Assert.True(connectionRecovered.WaitOne(Timeout));
            await using var producer = await connection.CreateAnonymousProducerAsync(CancellationToken);
        }

        [Fact]
        public async Task Throws_on_all_pending_attempts_to_create_when_connection_disposed_during_outage()
        {
            var host = CreateOpenedContainerHost();
            var connection = await CreateConnection(host.Endpoint);
            await DisposeHostAndWaitUntilConnectionNotified(host, connection);

            // The first command is picked up by the recovery loop, the second one stays queued behind it
            var createProducerTask = connection.CreateAnonymousProducerAsync(CancellationToken);
            var createConsumerTask = connection.CreateConsumerAsync("a1", RoutingType.Anycast, CancellationToken);
            await connection.DisposeAsync();

            await Assert.ThrowsAsync<ObjectDisposedException>(() => createProducerTask);
            await Assert.ThrowsAsync<ObjectDisposedException>(() => createConsumerTask);
            await Assert.ThrowsAsync<ObjectDisposedException>(() => connection.CreateAnonymousProducerAsync(CancellationToken));
        }

        [Fact]
        public async Task Should_allow_creating_producer_synchronously_from_ConnectionRecovered_handler()
        {
            var endpoint = GetUniqueEndpoint();
            var host1 = CreateOpenedContainerHost(endpoint);
            await using var connection = await CreateConnection(endpoint);
            var producerCreated = new ManualResetEvent(false);
            connection.ConnectionRecovered += (_, _) =>
            {
                using var cts = new CancellationTokenSource(Timeout);
                var producer = connection.CreateAnonymousProducerAsync(cts.Token).GetAwaiter().GetResult();
                producer.DisposeAsync().AsTask().GetAwaiter().GetResult();
                producerCreated.Set();
            };

            await DisposeHostAndWaitUntilConnectionNotified(host1, connection);
            using var host2 = CreateOpenedContainerHost(endpoint);

            Assert.True(producerCreated.WaitOne(Timeout));
        }

        [Fact]
        public async Task Should_recover_producer_when_connection_lost_during_recovery()
        {
            var endpoint = GetUniqueEndpoint();
            var host1 = CreateOpenedContainerHost(endpoint);
            await using var connection = await CreateConnection(endpoint);
            var producer = await connection.CreateAnonymousProducerAsync(CancellationToken);
            await DisposeHostAndWaitUntilConnectionNotified(host1, connection);

            // The second host accepts the connection, but never completes the producer's link attachment
            var attachReceived = new ManualResetEvent(false);
            // Install the handler before opening, so that the recovery can't attach before it's in place
            var host2 = CreateContainerHost(endpoint);
            host2.CreateTestLinkProcessor().SetHandler(_ =>
            {
                attachReceived.Set();
                return true;
            });
            host2.Open();
            Assert.True(attachReceived.WaitOne(Timeout));
            host2.Dispose();

            using var host3 = CreateOpenedContainerHost(endpoint);

            await producer.SendAsync("a1", new Message("foo"), CancellationToken);
            await producer.DisposeAsync();
        }

        [Fact]
        public async Task Should_recover_consumer_when_connection_lost_during_recovery()
        {
            var endpoint = GetUniqueEndpoint();
            var host1 = CreateOpenedContainerHost(endpoint);
            await using var connection = await CreateConnection(endpoint);
            var consumer = await connection.CreateConsumerAsync("a1", RoutingType.Anycast, CancellationToken);
            await DisposeHostAndWaitUntilConnectionNotified(host1, connection);

            // The second host accepts the connection, but never completes the consumer's link attachment
            var attachReceived = new ManualResetEvent(false);
            // Install the handler before opening, so that the recovery can't attach before it's in place
            var host2 = CreateContainerHost(endpoint);
            host2.CreateTestLinkProcessor().SetHandler(_ =>
            {
                attachReceived.Set();
                return true;
            });
            host2.Open();
            Assert.True(attachReceived.WaitOne(Timeout));
            host2.Dispose();

            // Register the message source before opening, so that the consumer can't attach to anything else
            using var host3 = CreateContainerHost(endpoint);
            var messageSource = host3.CreateMessageSource("a1");
            host3.Open();

            messageSource.Enqueue(new Message("foo"));

            var message = await consumer.ReceiveAsync(CancellationToken);
            Assert.Equal("foo", message.GetBody<string>());
            await consumer.DisposeAsync();
        }

        [Fact]
        public async Task Should_send_on_producer_resumed_on_connection_lost_later_during_recovery()
        {
            var endpoint = GetUniqueEndpoint();
            var host1 = CreateOpenedContainerHost(endpoint);
            await using var connection = await CreateConnection(endpoint);
            var producer1 = await connection.CreateAnonymousProducerAsync(CancellationToken);
            var producer2 = await connection.CreateAnonymousProducerAsync(CancellationToken);
            await DisposeHostAndWaitUntilConnectionNotified(host1, connection);

            // The second host completes the attachment of whichever producer is recovered first, so that it gets
            // resumed on this connection, but never completes the other one's
            var attachCount = 0;
            var secondAttachReceived = new ManualResetEvent(false);
            // Install the handler before opening, so that the recovery can't attach before it's in place
            var host2 = CreateContainerHost(endpoint);
            host2.CreateTestLinkProcessor().SetHandler(_ =>
            {
                if (Interlocked.Increment(ref attachCount) == 1)
                {
                    return false;
                }

                secondAttachReceived.Set();
                return true;
            });
            host2.Open();
            Assert.True(secondAttachReceived.WaitOne(Timeout));
            host2.Dispose();

            // One of the producers has already been recovered on the connection that is already gone
            var send1 = producer1.SendAsync("a1", new Message("foo"), CancellationToken);
            var send2 = producer2.SendAsync("a1", new Message("bar"), CancellationToken);

            using var host3 = CreateOpenedContainerHost(endpoint);

            await Task.WhenAll(send1, send2);
            await producer1.DisposeAsync();
            await producer2.DisposeAsync();
        }

        [Fact]
        public async Task Should_recover_producer_when_connection_closed_without_error_during_session_begin()
        {
            var endpoint = GetUniqueEndpoint();
            var host1 = CreateOpenedContainerHost(endpoint);
            await using var connection = await CreateConnection(endpoint);
            var producer = await connection.CreateAnonymousProducerAsync(CancellationToken);
            await DisposeHostAndWaitUntilConnectionNotified(host1, connection);

            // The peer accepts the connection, but closes it cleanly instead of answering the producer's begin
            using (var peer = new CloseOnBeginPeer(endpoint))
            {
                Assert.True(peer.CloseSent.WaitOne(Timeout));
            }

            using var host3 = CreateOpenedContainerHost(endpoint);

            await producer.SendAsync("a1", new Message("foo"), CancellationToken);
            await producer.DisposeAsync();
        }

        [Fact]
        public async Task Should_fail_creation_when_link_detached_without_error_and_keep_processing_commands()
        {
            var endpoint = GetUniqueEndpoint();
            var attachCount = 0;
            using var host = CreateContainerHost(endpoint);
            host.CreateTestLinkProcessor().SetHandler(context =>
            {
                if (Interlocked.Increment(ref attachCount) > 1)
                {
                    return false;
                }

                // Refuse the first attachment without an error: no source and target, followed by a clean detach
                context.Attach.Source = null;
                context.Attach.Target = null;
                context.Link.CompleteAttach(context.Attach, null);
                Task.Run(() => context.Link.Close(TimeSpan.Zero));
                return true;
            });
            host.Open();
            await using var connection = await CreateConnection(endpoint);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await Assert.ThrowsAsync<CreateProducerException>(() => connection.CreateProducerAsync("a1", RoutingType.Anycast, cts.Token));

            // The command loop isn't stuck on the refused attachment
            await using var producer = await connection.CreateProducerAsync("a1", RoutingType.Anycast, cts.Token);
        }

        [Fact]
        public async Task Should_create_producer_when_connection_closed_without_error_during_attach()
        {
            var endpoint = GetUniqueEndpoint();
            var attachCount = 0;
            using var host = CreateContainerHost(endpoint);
            host.CreateTestLinkProcessor().SetHandler(context =>
            {
                if (Interlocked.Increment(ref attachCount) > 1)
                {
                    return false;
                }

                // Leave the first attachment pending and close the connection without an error
                context.Link.Session.Connection.Close(TimeSpan.Zero, null);
                return true;
            });
            host.Open();
            await using var connection = await CreateConnection(endpoint);

            // No cancellation, the creation must not wait for an attachment that is never going to come
            var createTask = connection.CreateProducerAsync("a1", RoutingType.Anycast, CancellationToken.None);

            // The connection is reestablished as a part of the creation
            await using var producer = await createTask.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.Equal(2, Volatile.Read(ref attachCount));
        }

        [Fact]
        public async Task Should_wait_for_event_handlers_when_disposed()
        {
            var endpoint = GetUniqueEndpoint();
            var host1 = CreateOpenedContainerHost(endpoint);
            var connection = await CreateConnection(endpoint);
            var handlerStarted = new ManualResetEventSlim();
            var handlerFinished = false;
            connection.ConnectionRecovered += (_, _) =>
            {
                handlerStarted.Set();
                Thread.Sleep(ShortTimeout * 3);
                Volatile.Write(ref handlerFinished, true);
            };
            await DisposeHostAndWaitUntilConnectionNotified(host1, connection);
            using var host2 = CreateOpenedContainerHost(endpoint);
            Assert.True(handlerStarted.Wait(Timeout));

            await connection.DisposeAsync();

            Assert.True(Volatile.Read(ref handlerFinished));
        }

        [Fact]
        public async Task Should_dispose_connection_from_event_handler()
        {
            var endpoint = GetUniqueEndpoint();
            var host1 = CreateOpenedContainerHost(endpoint);
            var connection = await CreateConnection(endpoint);
            var disposed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            connection.ConnectionRecovered += (_, _) =>
            {
                // Blocks the handler until disposed, waiting for the handler itself would never finish
                connection.DisposeAsync().AsTask().GetAwaiter().GetResult();
                disposed.TrySetResult(true);
            };
            await DisposeHostAndWaitUntilConnectionNotified(host1, connection);
            using var host2 = CreateOpenedContainerHost(endpoint);

            await disposed.Task.WaitAsync(TimeSpan.FromSeconds(10));
        }

        [Fact]
        public async Task Should_give_up_when_connection_keeps_being_lost_during_recovery()
        {
            var endpoint = GetUniqueEndpoint();
            var host1 = CreateOpenedContainerHost(endpoint);
            var connectionFactory = CreateConnectionFactory();
            connectionFactory.RecoveryPolicy = RecoveryPolicyFactory.ConstantBackoff(TimeSpan.FromMilliseconds(10), retryCount: 3);
            await using var connection = await connectionFactory.CreateAsync(endpoint);
            var recoveryError = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
            connection.ConnectionRecoveryError += (_, args) => recoveryError.TrySetResult(args.Exception);
            var producer = await connection.CreateAnonymousProducerAsync(CancellationToken);
            await DisposeHostAndWaitUntilConnectionNotified(host1, connection);

            // The second host accepts every connection, but drops it as soon as the producer tries to attach
            using var host2 = CreateContainerHost(endpoint);
            host2.CreateTestLinkProcessor().SetHandler(context =>
            {
                var amqpConnection = context.Link.Session.Connection;
                Task.Run(() => amqpConnection.Close(TimeSpan.Zero, new Error(ErrorCode.ConnectionForced) { Description = "Dropped during recovery" }));
                return true;
            });
            host2.Open();

            // Each lost connection counts against the recovery policy, which eventually gives up
            await recoveryError.Task.WaitAsync(TimeSpan.FromSeconds(10));
            await Assert.ThrowsAsync<ProducerClosedException>(() => producer.SendAsync("a1", new Message("foo"), CancellationToken));
        }

        private static (TestContainerHost host, AutoResetEvent connected) CreateHost()
        {
            var endpoint = GetUniqueEndpoint();
            var connected = new AutoResetEvent(false);
            var handler = new TestHandler(@event =>
            {
                switch (@event.Id)
                {
                    case EventId.ConnectionRemoteOpen:
                        connected.Set();
                        break;
                }
            });
            var host = CreateOpenedContainerHost(endpoint, handler);
            return (host, connected);
        }
    }
}