using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.UnitTests.Utils;
using Amqp.Handler;
using Xunit;
using Xunit.Abstractions;

namespace ActiveMQ.Artemis.Client.UnitTests
{
    public class ProducerSendMessageSpec : ActiveMQNetSpec
    {
        public ProducerSendMessageSpec(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task Should_send_msg_and_wait_for_confirmation_from_the_server()
        {
            var endpoint = GetUniqueEndpoint();
            var deliveryReceived = new ManualResetEvent(false);
            var deliverySettled = false;

            var testHandler = new TestHandler(@event =>
            {
                switch (@event.Id)
                {
                    case EventId.ReceiveDelivery when @event.Context is IDelivery delivery:
                        deliverySettled = delivery.Settled;
                        deliveryReceived.Set();
                        break;
                }
            });

            using var host = CreateOpenedContainerHost(endpoint, testHandler);

            await using var connection = await CreateConnection(endpoint);
            var producer = await connection.CreateProducerAsync("a1", RoutingType.Anycast);

            await producer.SendAsync(new Message("foo"));

            Assert.True(deliveryReceived.WaitOne(Timeout));
            Assert.False(deliverySettled);
        }

        [Fact]
        public async Task Should_send_msg_with_Settled_delivery_frame_when_used_in_fire_and_forget_manner()
        {
            var endpoint = GetUniqueEndpoint();
            var deliveryReceived = new ManualResetEvent(false);
            var deliverySettled = false;

            var testHandler = new TestHandler(@event =>
            {
                switch (@event.Id)
                {
                    case EventId.ReceiveDelivery when @event.Context is IDelivery delivery:
                        deliverySettled = delivery.Settled;
                        deliveryReceived.Set();
                        break;
                }
            });

            using var host = CreateOpenedContainerHost(endpoint, testHandler);

            await using var connection = await CreateConnection(endpoint);
            var producer = await connection.CreateProducerAsync("a1", RoutingType.Anycast);

            producer.Send(new Message("foo"));

            Assert.True(deliveryReceived.WaitOne(Timeout));
            Assert.True(deliverySettled);
        }

        [Fact]
        public async Task Should_be_able_to_cancel_SendAsync_when_no_outcome_from_remote_peer_available()
        {
            var endpoint = GetUniqueEndpoint();

            using var host = CreateOpenedContainerHost(endpoint);
            var messageProcessor = host.CreateMessageProcessor("a1");

            // do not send outcome from a remote peer
            // as a result send should timeout
            messageProcessor.SetHandler(_ => true);

            await using var connection = await CreateConnection(endpoint);
            var producer = await connection.CreateProducerAsync("a1", RoutingType.Anycast);

            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromMilliseconds(50));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await producer.SendAsync(new Message("foo"), cts.Token));
        }

        [Theory, MemberData(nameof(RoutingTypesData))]
        public async Task Should_sent_message_using_specified_RoutingType(RoutingType routingType, object routingAnnotation)
        {
            var endpoint = GetUniqueEndpoint();

            using var host = CreateOpenedContainerHost(endpoint);
            var messageProcessor = host.CreateMessageProcessor("a1");

            await using var connection = await CreateConnection(endpoint);
            var producer = await connection.CreateProducerAsync("a1", routingType);

            await producer.SendAsync(new Message("foo"));

            var receivedMsg = messageProcessor.Dequeue(Timeout);

            Assert.Equal(routingAnnotation, receivedMsg.MessageAnnotations[SymbolUtils.RoutingType]);
        }
        
        [Fact]
        public async Task Should_sent_message_without_RoutingType()
        {
            var endpoint = GetUniqueEndpoint();

            using var host = CreateOpenedContainerHost(endpoint);
            var messageProcessor = host.CreateMessageProcessor("a1");

            await using var connection = await CreateConnection(endpoint);
            var producer = await connection.CreateProducerAsync("a1");

            await producer.SendAsync(new Message("foo"));

            var receivedMsg = messageProcessor.Dequeue(Timeout);

            Assert.Null(receivedMsg.MessageAnnotations[SymbolUtils.RoutingType]);
        }

        public static IEnumerable<object[]> RoutingTypesData()
        {
            return new[]
            {
                new object[] { RoutingType.Multicast, (sbyte) 0 },
                new object[] { RoutingType.Anycast, (sbyte) 1 },
            };
        }
    }
}