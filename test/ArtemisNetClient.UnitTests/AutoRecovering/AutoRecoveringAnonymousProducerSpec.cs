using System;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.AutoRecovering;
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
    public class AutoRecoveringAnonymousProducerSpec : ActiveMQNetSpec
    {
        public AutoRecoveringAnonymousProducerSpec(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task Should_not_suspend_anonymous_producer_when_send_fails_on_producer_already_replaced()
        {
            var endpoint = GetUniqueEndpoint();
            var messageCount = 0;
            var message1Received = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var message2Received = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var host = CreateContainerHost(endpoint);
            host.CreateTestLinkProcessor().SetHandler(context =>
            {
                context.Complete(new NeverSettlingLinkEndpoint(() =>
                {
                    var message = Interlocked.Increment(ref messageCount) == 1 ? message1Received : message2Received;
                    message.TrySetResult(true);
                }), 30);
                return true;
            });
            host.Open();
            await using var connection = await CreateConnectionWithoutAutomaticRecovery(endpoint);
            await using var producer = new AutoRecoveringAnonymousProducer(NullLoggerFactory.Instance, new AnonymousProducerConfiguration());
            var recoveryRequested = false;
            producer.RecoveryRequested += () => recoveryRequested = true;
            await producer.RecoverAsync(connection, CancellationToken);

            using var cts = new CancellationTokenSource(Timeout);
            var sendTask = producer.SendAsync("a1", new Message("foo"), cts.Token);
            await message1Received.Task.WaitAsync(Timeout);

            // Replacing the producer disposes the old one, which fails the send pending on it. The failure comes
            // late, the producer has already been replaced.
            await producer.RecoverAsync(connection, CancellationToken);

            // The message is sent again over the replacement, which is left as it is
            await message2Received.Task.WaitAsync(Timeout);
            Assert.False(producer.IsSuspended);
            Assert.False(recoveryRequested);

            // Nobody is going to settle it
            await cts.CancelAsync();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sendTask);
        }

        [Fact]
        public async Task Should_not_terminate_anonymous_producer_when_send_fails_with_terminal_error_on_producer_already_replaced()
        {
            var producer1 = new FakeAnonymousProducer();
            var producer2 = new FakeAnonymousProducer();
            var connection = new FakeConnection(producer1, producer2);
            await using var producer = new AutoRecoveringAnonymousProducer(NullLoggerFactory.Instance, new AnonymousProducerConfiguration());
            await producer.RecoverAsync(connection, CancellationToken);

            var sendTask = producer.SendAsync("a1", new Message("foo"), CancellationToken);
            await producer1.MessageSent.Task.WaitAsync(Timeout);
            await producer.RecoverAsync(connection, CancellationToken);

            // The send pending on the replaced producer fails late
            producer1.PendingSend.SetException(new ProducerClosedException("Unauthorized", ErrorCode.UnauthorizedAccess, null));

            // It's sent again over the replacement, which is left as it is
            await producer2.MessageSent.Task.WaitAsync(Timeout);
            producer2.PendingSend.SetResult(true);
            await sendTask.WaitAsync(Timeout);
            Assert.False(producer2.IsDisposed);
        }

        [Fact]
        public async Task Should_fail_send_waiting_for_recovery_when_anonymous_producer_disposed()
        {
            var underlyingProducer = new FakeAnonymousProducer();
            var producer = new AutoRecoveringAnonymousProducer(NullLoggerFactory.Instance, new AnonymousProducerConfiguration());
            var recoveryRequested = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            producer.RecoveryRequested += () => recoveryRequested.TrySetResult(true);
            await producer.RecoverAsync(new FakeConnection(underlyingProducer), CancellationToken);

            var sendTask = producer.SendAsync("a1", new Message("foo"), CancellationToken);
            await underlyingProducer.MessageSent.Task.WaitAsync(Timeout);

            // The send waits for the recovery
            underlyingProducer.PendingSend.SetException(new ProducerClosedException());
            await recoveryRequested.Task.WaitAsync(Timeout);

            await producer.DisposeAsync();

            // There is not going to be any recovery, the send must not wait for it
            await Assert.ThrowsAsync<ObjectDisposedException>(() => sendTask.WaitAsync(Timeout));
        }

        [Fact]
        public async Task Should_not_suspend_anonymous_producer_once_disposed()
        {
            var connection = new FakeConnection(new FakeAnonymousProducer());
            var producer = new AutoRecoveringAnonymousProducer(NullLoggerFactory.Instance, new AnonymousProducerConfiguration());
            await producer.RecoverAsync(connection, CancellationToken);
            await producer.DisposeAsync();

            // E.g. the connection suspending all its recoverables at the same time
            producer.Suspend();

            Assert.False(producer.IsSuspended);
        }

        private class NeverSettlingLinkEndpoint(Action onMessage) : LinkEndpoint
        {
            public override void OnMessage(MessageContext messageContext) => onMessage();

            public override void OnFlow(FlowContext flowContext)
            {
            }

            public override void OnDisposition(DispositionContext dispositionContext) => dispositionContext.Complete();
        }

        [Fact]
        public async Task Should_terminate_anonymous_producer_on_SendAsync_when_link_closed_with_unauthorized_access()
        {
            // The client replies to the broker's detach only after it has processed it
            var producerDetached = new ManualResetEvent(false);
            var testHandler = new TestHandler(@event =>
            {
                if (@event.Id == EventId.LinkRemoteClose)
                {
                    producerDetached.Set();
                }
            });
            using var host = CreateOpenedContainerHost(handler: testHandler);
            var linkProcessor = host.CreateTestLinkProcessor();

            ListenerLink producerLink = null;
            linkProcessor.SetHandler(context =>
            {
                producerLink = context.Link;
                return false;
            });

            await using var connection = await CreateConnection(host.Endpoint);
            var producer = await connection.CreateAnonymousProducerAsync();

            try
            {
                await producerLink.CloseAsync(Timeout, new Error(ErrorCode.UnauthorizedAccess) { Description = "Unauthorized" });
            }
            catch (Exception)
            {
                // ignored
            }

            // CloseAsync throws without waiting for the client, wait until the client has seen the detach
            Assert.True(producerDetached.WaitOne(Timeout));

            var exception = await Assert.ThrowsAsync<ProducerClosedException>(() =>
                producer.SendAsync("a1", null, new Message("foo"), null, CancellationToken));
            Assert.Equal(ErrorCode.UnauthorizedAccess, exception.ErrorCode);

            await Assert.ThrowsAsync<ProducerClosedException>(() =>
                producer.SendAsync("a2", null, new Message("bar"), null, CancellationToken));
        }

        [Fact]
        public async Task Should_terminate_anonymous_producer_on_Send_when_link_closed_with_unauthorized_access()
        {
            // The client replies to the broker's detach only after it has processed it
            var producerDetached = new ManualResetEvent(false);
            var testHandler = new TestHandler(@event =>
            {
                if (@event.Id == EventId.LinkRemoteClose)
                {
                    producerDetached.Set();
                }
            });
            using var host = CreateOpenedContainerHost(handler: testHandler);
            var linkProcessor = host.CreateTestLinkProcessor();

            ListenerLink producerLink = null;
            linkProcessor.SetHandler(context =>
            {
                producerLink = context.Link;
                return false;
            });

            await using var connection = await CreateConnection(host.Endpoint);
            var producer = await connection.CreateAnonymousProducerAsync();

            try
            {
                await producerLink.CloseAsync(Timeout, new Error(ErrorCode.UnauthorizedAccess) { Description = "Unauthorized" });
            }
            catch (Exception)
            {
                // ignored
            }

            // CloseAsync throws without waiting for the client, wait until the client has seen the detach
            Assert.True(producerDetached.WaitOne(Timeout));

            var exception = Assert.Throws<ProducerClosedException>(() =>
                producer.Send("a1", null, new Message("foo"), CancellationToken));
            Assert.Equal(ErrorCode.UnauthorizedAccess, exception.ErrorCode);

            Assert.Throws<ProducerClosedException>(() =>
                producer.Send("a2", null, new Message("bar"), CancellationToken));
        }
        [Fact]
        public async Task Should_not_recreate_anonymous_producer_on_connection_recovery_after_terminal_unauthorized_access_error()
        {
            var endpoint = GetUniqueEndpoint();
            var producerAttached = new ManualResetEvent(false);
            var producerDetached = new ManualResetEvent(false);
            var testHandler = new TestHandler(@event =>
            {
                switch (@event.Id)
                {
                    case EventId.LinkRemoteOpen when @event.Context is Attach attach && attach.Role:
                        producerAttached.Set();
                        break;
                    // The client replies to the broker's detach only after it has processed it
                    case EventId.LinkRemoteClose:
                        producerDetached.Set();
                        break;
                }
            });

            var host1 = CreateOpenedContainerHost(endpoint, testHandler);
            var linkProcessor = host1.CreateTestLinkProcessor();

            ListenerLink producerLink = null;
            linkProcessor.SetHandler(context =>
            {
                producerLink = context.Link;
                return false;
            });

            var connection = await CreateConnection(endpoint);
            var producer = await connection.CreateAnonymousProducerAsync();

            Assert.True(producerAttached.WaitOne(Timeout));
            producerAttached.Reset();

            try
            {
                await producerLink.CloseAsync(Timeout, new Error(ErrorCode.UnauthorizedAccess) { Description = "Unauthorized" });
            }
            catch (Exception)
            {
                // ignored
            }

            // CloseAsync throws without waiting for the client, wait until the client has seen the detach
            Assert.True(producerDetached.WaitOne(Timeout));

            // SendAsync triggers TerminateAsync which should remove the producer from the recovery set.
            await Assert.ThrowsAsync<ProducerClosedException>(() =>
                producer.SendAsync("a1", null, new Message("foo"), null, CancellationToken));

            host1.Dispose();
            using var host2 = CreateOpenedContainerHost(endpoint, testHandler);

            // Producer should NOT be recreated after connection recovery.
            Assert.False(producerAttached.WaitOne(ShortTimeout));

            await DisposeUtil.DisposeAll(connection, host2);
        }
    }
}
