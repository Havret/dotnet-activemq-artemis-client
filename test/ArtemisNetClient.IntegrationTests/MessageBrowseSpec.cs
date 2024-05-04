using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace ActiveMQ.Artemis.Client.IntegrationTests
{
    public class MessageBrowseSpec : ActiveMQNetIntegrationSpec
    {
        public MessageBrowseSpec(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task Should_find_message_by_browsing_for_id_property_with_existing_id_and_single_message()
        {
            await using var connection = await CreateConnection();
            var address = Guid.NewGuid().ToString();
            await using var producer = await connection.CreateProducerAsync(address, RoutingType.Anycast);

            var messageId = Guid.NewGuid().ToString();
            var message = new Message("foo");
            message.ApplicationProperties["id"] = messageId;

            await producer.SendAsync(message);

            var configuration = new ConsumerConfiguration
            {
                Address = address,
                RoutingType = RoutingType.Anycast,
                FilterExpression = $"id = '{messageId}'",
            };

            await using var browser = await connection.CreateBrowserAsync(configuration);


            Message existingMessage = null;
            if (browser.MoveNext())
            {
                existingMessage = browser.Current;
            }

            Assert.Equal("foo", existingMessage.GetBody<string>());
        }

        [Fact]
        public async Task Should_find_message_by_browsing_for_id_property_with_existing_id_and_multiple_messages()
        {
            await using var connection = await CreateConnection();
            var address = Guid.NewGuid().ToString();
            await using var producer = await connection.CreateProducerAsync(address, RoutingType.Anycast);

            var messageId = Guid.NewGuid().ToString();
            var message = new Message("foo");
            message.ApplicationProperties["id"] = messageId;

            await producer.SendAsync(new Message(Guid.NewGuid().ToString()));
            await producer.SendAsync(new Message(Guid.NewGuid().ToString()));

            await producer.SendAsync(message);
            
            await producer.SendAsync(new Message(Guid.NewGuid().ToString()));
            await producer.SendAsync(new Message(Guid.NewGuid().ToString()));

            var configuration = new ConsumerConfiguration
            {
                Address = address,
                RoutingType = RoutingType.Anycast,
                FilterExpression = $"id = '{messageId}'",
            };

            await using var browser = await connection.CreateBrowserAsync(configuration);


            Message existingMessage = null;
            if (browser.MoveNext())
            {
                existingMessage = browser.Current;
            }

            Assert.Equal("foo", existingMessage.GetBody<string>());
        }

        [Fact]
        public async Task Should_not_find_message_by_browsing_for_id_property_with_inexisting_id()
        {
            await using var connection = await CreateConnection();
            var address = Guid.NewGuid().ToString();
            await using var producer = await connection.CreateProducerAsync(address, RoutingType.Anycast);

            var message = new Message("foo");
            message.ApplicationProperties["id"] = Guid.NewGuid().ToString();

            await producer.SendAsync(message);

            var configuration = new ConsumerConfiguration
            {
                Address = address,
                RoutingType = RoutingType.Anycast,
                FilterExpression = $"id = '{Guid.NewGuid()}'", // inexistent id
            };

            await using var browser = await connection.CreateBrowserAsync(configuration);

            Message maybeMessage = null;
            if (browser.MoveNext())
            {
                maybeMessage = browser.Current;
            }

            Assert.Null(maybeMessage);
        }

        [Fact]
        public async Task Should_not_find_message_by_browsing_for_id_property_with_empty_id()
        {
            await using var connection = await CreateConnection();
            var address = Guid.NewGuid().ToString();
            await using var producer = await connection.CreateProducerAsync(address, RoutingType.Anycast);

            var message = new Message("foo");
            message.ApplicationProperties["id"] = Guid.NewGuid().ToString();

            await producer.SendAsync(message);

            var configuration = new ConsumerConfiguration
            {
                Address = address,
                RoutingType = RoutingType.Anycast,
                FilterExpression = $"id = ''", // empty id
            };

            await using var browser = await connection.CreateBrowserAsync(configuration);

            Message maybeMessage = null;
            if (browser.MoveNext())
            {
                maybeMessage = browser.Current;
            }

            Assert.Null(maybeMessage);
        }
    }
}