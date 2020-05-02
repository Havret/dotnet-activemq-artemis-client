using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Net.TestUtils;
using Xunit;
using Xunit.Abstractions;

namespace ActiveMQ.Net.IntegrationTests
{
    public class FilterExpressionsSpec : ActiveMQNetIntegrationSpec
    {
        public FilterExpressionsSpec(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task Should_filter_messages_based_on_priority()
        {
            await using var connection = await CreateConnection();
            var address = nameof(Should_filter_messages_based_on_priority);
            await using var producer = await connection.CreateProducerAsync(address, AddressRoutingType.Anycast);
            await using var highPriorityConsumer = await connection.CreateConsumerAsync(new ConsumerConfiguration
            {
                Address = address,
                RoutingType = QueueRoutingType.Anycast,
                FilterExpression = "AMQPriority = 9"
            });
            await using var lowPriorityConsumer = await connection.CreateConsumerAsync(new ConsumerConfiguration
            {
                Address = address,
                RoutingType = QueueRoutingType.Anycast,
                FilterExpression = "AMQPriority = 0"
            });

            for (int i = 0; i < 3; i++)
            {
                await producer.SendAsync(new Message("highPriorityMsg") { Priority = 9 });
                await producer.SendAsync(new Message("lowPriorityMsg") { Priority = 0 });
            }

            var lowPriorityMessages = await ReceiveMessages(lowPriorityConsumer, 3);
            var highPriorityMessages = await ReceiveMessages(highPriorityConsumer, 3);

            Assert.All(lowPriorityMessages, x => Assert.Equal("lowPriorityMsg", x.GetBody<string>()));
            Assert.All(highPriorityMessages, x => Assert.Equal("highPriorityMsg", x.GetBody<string>()));
        }

        [Fact]
        public async Task Should_filter_messages_based_on_message_expiration_time()
        {
            await using var connection = await CreateConnection();
            var address = nameof(Should_filter_messages_based_on_message_expiration_time);
            await using var producer = await connection.CreateProducerAsync(address, AddressRoutingType.Anycast);

            var messageExpiryTime = DateTimeOffset.UtcNow.AddMilliseconds(100_000).ToUnixTimeMilliseconds();

            await using var shortExpiryConsumer = await connection.CreateConsumerAsync(new ConsumerConfiguration
            {
                Address = address,
                RoutingType = QueueRoutingType.Anycast,
                FilterExpression = $"AMQExpiration < {messageExpiryTime}"
            });
            await using var longExpiryConsumer = await connection.CreateConsumerAsync(new ConsumerConfiguration
            {
                Address = address,
                RoutingType = QueueRoutingType.Anycast,
                FilterExpression = $"AMQExpiration > {messageExpiryTime}"
            });

            for (int i = 0; i < 3; i++)
            {
                await producer.SendAsync(new Message("longExpiryMsg") { TimeToLive = TimeSpan.FromMilliseconds(200_000) });
                await producer.SendAsync(new Message("shortExpiryMsg") { TimeToLive = TimeSpan.FromMilliseconds(50_000) });
            }

            var longExpiryMessages = await ReceiveMessages(longExpiryConsumer, 3);
            var shortExpiryMessages = await ReceiveMessages(shortExpiryConsumer, 3);

            Assert.All(longExpiryMessages, x => Assert.Equal("longExpiryMsg", x.GetBody<string>()));
            Assert.All(shortExpiryMessages, x => Assert.Equal("shortExpiryMsg", x.GetBody<string>()));
        }

        [Fact]
        public async Task Should_filter_messages_based_on_message_durability()
        {
            await using var connection = await CreateConnection();
            var address = nameof(Should_filter_messages_based_on_message_durability);
            await using var producer = await connection.CreateProducerAsync(address, AddressRoutingType.Anycast);

            await using var durableMessageConsumer = await connection.CreateConsumerAsync(new ConsumerConfiguration
            {
                Address = address,
                RoutingType = QueueRoutingType.Anycast,
                FilterExpression = "AMQDurable = 'DURABLE'"
            });
            await using var nondurableConsumer = await connection.CreateConsumerAsync(new ConsumerConfiguration
            {
                Address = address,
                RoutingType = QueueRoutingType.Anycast,
                FilterExpression = "AMQDurable = 'NON_DURABLE'"
            });

            for (int i = 0; i < 3; i++)
            {
                await producer.SendAsync(new Message("durableMsg") { DurabilityMode = DurabilityMode.Durable });
                await producer.SendAsync(new Message("nondurableMsg") { DurabilityMode = DurabilityMode.Nondurable });
            }

            var nondurableMessages = await ReceiveMessages(nondurableConsumer, 3);
            var durableMessages = await ReceiveMessages(durableMessageConsumer, 3);

            Assert.All(nondurableMessages, x => Assert.Equal("nondurableMsg", x.GetBody<string>()));
            Assert.All(durableMessages, x => Assert.Equal("durableMsg", x.GetBody<string>()));
        }

        [Fact]
        public async Task Should_filter_messages_based_on_message_creation_time()
        {
            await using var connection = await CreateConnection();
            var address = nameof(Should_filter_messages_based_on_message_creation_time);
            await using var producer = await connection.CreateProducerAsync(address, AddressRoutingType.Anycast);

            var dayAgo = DateTime.UtcNow.AddDays(-1).DropTicsPrecision();
            var now = DateTime.UtcNow.DropTicsPrecision();

            await using var staleMessageConsumer = await connection.CreateConsumerAsync(new ConsumerConfiguration
            {
                Address = address,
                RoutingType = QueueRoutingType.Anycast,
                FilterExpression = $"AMQTimestamp = {dayAgo.ToUnixTimeMilliseconds()}"
            });
            await using var freshMessageConsumer = await connection.CreateConsumerAsync(new ConsumerConfiguration
            {
                Address = address,
                RoutingType = QueueRoutingType.Anycast,
                FilterExpression = $"AMQTimestamp = {now.ToUnixTimeMilliseconds()}"
            });

            for (int i = 0; i < 3; i++)
            {
                await producer.SendAsync(new Message("staleMessage") { CreationTime = dayAgo });
                await producer.SendAsync(new Message("freshMessage") { CreationTime = now });
            }

            var freshMessages = await ReceiveMessages(freshMessageConsumer, 3);
            var staleMessages = await ReceiveMessages(staleMessageConsumer, 3);

            Assert.All(freshMessages, x => Assert.Equal("freshMessage", x.GetBody<string>()));
            Assert.All(staleMessages, x => Assert.Equal("staleMessage", x.GetBody<string>()));
        }

        [Fact]
        public async Task Should_filter_messages_based_on_application_property()
        {
            await using var connection = await CreateConnection();
            var address = nameof(Should_filter_messages_based_on_application_property);
            await using var producer = await connection.CreateProducerAsync(address, AddressRoutingType.Anycast);

            await using var redMessageConsumer = await connection.CreateConsumerAsync(new ConsumerConfiguration
            {
                Address = address,
                RoutingType = QueueRoutingType.Anycast,
                FilterExpression = "color = 'red'"
            });
            await using var blueMessageConsumer = await connection.CreateConsumerAsync(new ConsumerConfiguration
            {
                Address = address,
                RoutingType = QueueRoutingType.Anycast,
                FilterExpression = "color = 'blue'"
            });

            for (int i = 0; i < 3; i++)
            {
                await producer.SendAsync(new Message("red") { ApplicationProperties = { ["color"] = "red" } });
                await producer.SendAsync(new Message("blue") { ApplicationProperties = { ["color"] = "blue" } });
            }

            var redMessages = await ReceiveMessages(redMessageConsumer, 3);
            var blueMessages = await ReceiveMessages(blueMessageConsumer, 3);

            Assert.All(redMessages, x => Assert.Equal("red", x.GetBody<string>()));
            Assert.All(blueMessages, x => Assert.Equal("blue", x.GetBody<string>()));
        }


        private static async Task<IReadOnlyList<Message>> ReceiveMessages(IConsumer consumer, int count)
        {
            var messages = new List<Message>();
            for (int i = 0; i < count; i++)
            {
                var message = await consumer.ReceiveAsync(CancellationToken);
                messages.Add(message);
            }

            return messages;
        }
    }
}