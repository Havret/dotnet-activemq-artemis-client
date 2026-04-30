using System;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Exceptions;
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
    }
}
