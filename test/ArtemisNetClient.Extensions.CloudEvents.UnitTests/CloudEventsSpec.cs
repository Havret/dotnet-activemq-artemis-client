using System;
using System.Globalization;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Testing;
using ActiveMQ.Artemis.Client.TestUtils;
using CloudNative.CloudEvents;
using CloudNative.CloudEvents.SystemTextJson;
using Xunit;

namespace ActiveMQ.Artemis.Client.Extensions.CloudEvents.UnitTests;

public class CloudEventsSpec
{
    [Theory]
    [InlineData(ContentMode.Binary)]
    [InlineData(ContentMode.Structured)]
    public async Task Should_convert_cloud_event_to_message(ContentMode contentMode)
    {
        // Arrange
        var cloudEvent = new CloudEvent
        {
            Type = "com.github.pull.create",
            Source = new Uri("https://github.com/cloudevents/spec/pull"),
            Subject = "123",
            Id = "A234-1234-1234",
            Time = new DateTimeOffset(2018, 4, 5, 17, 31, 0, TimeSpan.Zero),
            DataContentType = MediaTypeNames.Text.Xml,
            Data = "<much wow=\"xml\"/>",
            ["comexampleextension1"] = "value"
        };
        
        // Act
        var message = cloudEvent.ToActiveMqMessage(contentMode, new JsonEventFormatter());
        var receivedMessage = await SendAndReceiveAsync(message);

        // Assert
        Assert.True(receivedMessage.IsCloudEvent());
        var receivedCloudEvent = receivedMessage.ToCloudEvent(new JsonEventFormatter());
        AssertCloudEventsEqual(cloudEvent, receivedCloudEvent);
    }
    
    [Theory]
    [InlineData(ContentMode.Binary)]
    [InlineData(ContentMode.Structured)]
    public async Task Should_convert_cloud_event_to_message_with_colon_prefix(ContentMode contentMode)
    {
        // Arrange
        var cloudEvent = new CloudEvent
        {
            Type = "com.github.pull.create",
            Source = new Uri("https://github.com/cloudevents/spec/pull"),
            Subject = "123",
            Id = "A234-1234-1234",
            Time = new DateTimeOffset(2018, 4, 5, 17, 31, 0, TimeSpan.Zero),
            DataContentType = MediaTypeNames.Text.Xml,
            Data = "<much wow=\"xml\"/>",
            ["comexampleextension1"] = "value"
        };
        
        // Act
        var message = cloudEvent.ToActiveMqMessageWithColonPrefix(contentMode, new JsonEventFormatter());
        var receivedMessage = await SendAndReceiveAsync(message);

        // Assert
        Assert.True(receivedMessage.IsCloudEvent());
        var receivedCloudEvent = receivedMessage.ToCloudEvent(new JsonEventFormatter());
        AssertCloudEventsEqual(cloudEvent, receivedCloudEvent);
    }

    [Fact]
    public async Task Should_normalize_timestamps_to_utc()
    {
        // Arrange
        var cloudEvent = new CloudEvent
        {
            Type = "com.github.pull.create",
            Source = new Uri("https://github.com/cloudevents/spec/pull"),
            Id = "A234-1234-1234",
            // 2018-04-05T18:31:00+01:00 => 2018-04-05T17:31:00Z
            Time = new DateTimeOffset(2018, 4, 5, 18, 31, 0, TimeSpan.FromHours(1))
        };
        
        // Act
        var message = cloudEvent.ToActiveMqMessageWithColonPrefix(ContentMode.Binary, new JsonEventFormatter());
        var receivedMessage = await SendAndReceiveAsync(message);

        // Assert
        var receivedCloudEvent = receivedMessage.ToCloudEvent(new JsonEventFormatter());
        var expected = DateTimeOffset.ParseExact("2018-04-05T17:31:00Z", "yyyy-MM-dd'T'HH:mm:ss.FFFFFFFK", CultureInfo.InvariantCulture);
        Assert.Equal(expected, receivedCloudEvent.Time);
    }

    private static async Task<Message> SendAndReceiveAsync(Message message)
    {
        var endpoint = EndpointUtil.GetUniqueEndpoint();
        var testAddress = Guid.NewGuid().ToString();
        
        using var testKit = new TestKit(endpoint);
        var subscription = testKit.Subscribe(testAddress);

        var connectionFactory = new ConnectionFactory();
        await using var connection = await connectionFactory.CreateAsync(endpoint);
        var producer = await connection.CreateProducerAsync(testAddress);
        await producer.SendAsync(message);

        return await subscription.ReceiveAsync();
    }

    private static void AssertCloudEventsEqual(CloudEvent expected, CloudEvent actual)
    {
        Assert.Equal(expected.SpecVersion, actual.SpecVersion);
        var expectedAttributes = expected.GetPopulatedAttributes().ToList();
        var actualAttributes = actual.GetPopulatedAttributes().ToList();

        Assert.Equal(expectedAttributes.Count, actualAttributes.Count);
        foreach (var expectedAttribute in expectedAttributes)
        {
            var actualAttribute = actualAttributes.FirstOrDefault(actual => actual.Key.Name == expectedAttribute.Key.Name);
            Assert.NotNull(actualAttribute.Key);
            Assert.Equal(actualAttribute.Key.Type, expectedAttribute.Key.Type);
            Assert.Equal(actualAttribute.Value, expectedAttribute.Value);
        }
    }
}