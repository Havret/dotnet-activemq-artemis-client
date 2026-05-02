using System;
using System.Threading;
using System.Threading.Tasks;
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
    public class AutoRecoveringAnonymousProducerSpec : ActiveMQNetSpec
    {
        public AutoRecoveringAnonymousProducerSpec(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task Should_terminate_anonymous_producer_on_SendAsync_when_link_closed_with_unauthorized_access()
        {
            using var host = CreateOpenedContainerHost();
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

            var exception = await Assert.ThrowsAsync<ProducerClosedException>(() =>
                producer.SendAsync("a1", null, new Message("foo"), null, CancellationToken));
            Assert.Equal(ErrorCode.UnauthorizedAccess, exception.ErrorCode);

            await Assert.ThrowsAsync<ProducerClosedException>(() =>
                producer.SendAsync("a2", null, new Message("bar"), null, CancellationToken));
        }

        [Fact]
        public async Task Should_terminate_anonymous_producer_on_Send_when_link_closed_with_unauthorized_access()
        {
            using var host = CreateOpenedContainerHost();
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
