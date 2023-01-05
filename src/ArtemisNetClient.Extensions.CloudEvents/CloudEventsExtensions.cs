using System;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Runtime.InteropServices;
using CloudNative.CloudEvents;
using CloudNative.CloudEvents.Core;

namespace ActiveMQ.Artemis.Client.Extensions.CloudEvents;

/// <summary>
/// Extension methods to convert between CloudEvents and ArtemisNetClient messages.
/// </summary>
public static class CloudEventsExtensions
{
    private const string SpecVersionAttributeName = "specversion";

    private const string AmqpHeaderUnderscorePrefix = "cloudEvents_";
    private const string AmqpHeaderColonPrefix = "cloudEvents:";

    private const string SpecVersionAmqpHeaderWithUnderscore = AmqpHeaderUnderscorePrefix + SpecVersionAttributeName;
    private const string SpecVersionAmqpHeaderWithColon = AmqpHeaderColonPrefix + SpecVersionAttributeName;

    /// <summary>
    /// Converts a CloudEvent to <see cref="Message"/> using the default property prefix property prefix of "cloudEvents_".
    /// </summary>
    /// <param name="cloudEvent">The CloudEvent to convert. Must not be null, and must be a valid CloudEvent.</param>
    /// <param name="contentMode">Content mode. Structured or binary.</param>
    /// <param name="formatter">The formatter to use within the conversion. Must not be null.</param>
    public static Message ToActiveMqMessage(this CloudEvent cloudEvent, ContentMode contentMode, CloudEventFormatter formatter)
    {
        return ToActiveMqMessage(cloudEvent, contentMode, formatter, AmqpHeaderColonPrefix);
    }

    /// <summary>
    /// Converts a CloudEvent to <see cref="Message"/> using a property prefix of "cloudEvents:". This prefix
    /// is a legacy retained only for compatibility purposes; it can't be used by JMS due to constraints in JMS property names.
    /// </summary>
    /// <param name="cloudEvent">The CloudEvent to convert. Must not be null, and must be a valid CloudEvent.</param>
    /// <param name="contentMode">Content mode. Structured or binary.</param>
    /// <param name="formatter">The formatter to use within the conversion. Must not be null.</param>
    public static Message ToActiveMqMessageWithColonPrefix(this CloudEvent cloudEvent, ContentMode contentMode, CloudEventFormatter formatter)
    {
        return ToActiveMqMessage(cloudEvent, contentMode, formatter, AmqpHeaderColonPrefix);
    }

    private static Message ToActiveMqMessage(CloudEvent cloudEvent, ContentMode contentMode, CloudEventFormatter formatter, string prefix)
    {
        Message message;
        switch (contentMode)
        {
            case ContentMode.Binary:
            {
                var memory = formatter.EncodeBinaryModeEventData(cloudEvent);
                var payload = MemoryMarshal.TryGetArray(memory, out var segment)
                    ? segment.Array
                    : memory.ToArray();
                message = new Message(payload)
                {
                    ContentType = formatter.GetOrInferDataContentType(cloudEvent),
                };
                break;
            }
            case ContentMode.Structured:
            {
                var memory = formatter.EncodeStructuredModeMessage(cloudEvent, out var contentType);
                var payload = MemoryMarshal.TryGetArray(memory, out var segment)
                    ? segment.Array
                    : memory.ToArray();
                // TODO: What about the other parts of the content type?
                message = new Message(payload)
                {
                    ContentType = contentType.MediaType,
                };
                break;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(contentMode), $"Unsupported content mode: {contentMode}");
        }
        
        message.ApplicationProperties[prefix + SpecVersionAttributeName] = cloudEvent.SpecVersion.VersionId;

        foreach (var pair in cloudEvent.GetPopulatedAttributes())
        {
            var attribute = pair.Key;
            var value = pair.Value;

            // The content type is specified elsewhere.
            if (attribute == cloudEvent.SpecVersion.DataContentTypeAttribute)
            {
                continue;
            }

            var propKey = prefix + attribute.Name;
            var propValue = value switch
            {
                Uri uri => uri.ToString(),
                // AMQPNetLite doesn't support DateTimeOffset values, so convert to UTC.
                // That means we can't roundtrip events with non-UTC timestamps, but that's not awful.
                DateTimeOffset dto => dto.UtcDateTime,
                _ => value
            };

            message.ApplicationProperties[propKey] = propValue;
        }
        
        return message;
    }

    /// <summary>
    /// Converts this ActiveMQ message into a CloudEvent object.
    /// </summary>
    /// <param name="message">The ActiveMQ message to convert. Must not be null.</param>
    /// <param name="formatter">The event formatter to use to parse the CloudEvent. Must not be null.</param>
    /// <param name="extensionAttributes">The extension attributes to use when parsing the CloudEvent. May be null.</param>
    /// <returns>A reference to a validated CloudEvent instance.</returns>
    public static CloudEvent ToCloudEvent(this Message message, CloudEventFormatter formatter, params CloudEventAttribute[] extensionAttributes)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));
        if (formatter == null) throw new ArgumentNullException(nameof(formatter));

        if (MimeUtilities.IsCloudEventsContentType(message.ContentType))
        {
            return formatter.DecodeStructuredModeMessage(new MemoryStream(message.GetBody<byte[]>()), new ContentType(message.ContentType), extensionAttributes);
        }

        if (!message.ApplicationProperties.TryGetValue(SpecVersionAmqpHeaderWithUnderscore, out string versionId) &&
            !message.ApplicationProperties.TryGetValue(SpecVersionAmqpHeaderWithColon, out versionId))
        {
            throw new ArgumentException("Message is not a CloudEvent");
        }

        var version = CloudEventsSpecVersion.FromVersionId(versionId)
                      ?? throw new ArgumentException($"Unknown CloudEvents spec version '{versionId}'", nameof(message));

        var cloudEvent = new CloudEvent(version, extensionAttributes)
        {
            DataContentType = message.ContentType
        };
        foreach (var key in message.ApplicationProperties.Keys)
        {
            if (TryGetAttributeName(key, out string attributeName) == false)
            {
                continue;
            }

            // We've already dealt with the spec version.
            if (attributeName == CloudEventsSpecVersion.SpecVersionAttribute.Name)
            {
                continue;
            }

            var value = message.ApplicationProperties[key];

            // Timestamps are serialized via DateTime instead of DateTimeOffset.
            if (value is DateTime dateTime)
            {
                if (dateTime.Kind != DateTimeKind.Utc)
                {
                    // This should only happen for MinValue and MaxValue...
                    // just respecify as UTC. (We could add validation that it really
                    // *is* MinValue or MaxValue if we wanted to.)
                    dateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
                }

                cloudEvent[attributeName] = (DateTimeOffset) dateTime;
            }
            // URIs are serialized as strings, but we need to convert them back to URIs.
            // It's simplest to let CloudEvent do this for us.
            else if (value is string text)
            {
                cloudEvent.SetAttributeFromString(attributeName, text);
            }
            else
            {
                cloudEvent[attributeName] = value;
            }

            // Populate the data after the rest of the CloudEvent
            if (message.GetBody<byte[]>() is { } payload)
            {
                formatter.DecodeBinaryModeEventData(payload, cloudEvent);
            }
        }

        if (cloudEvent.IsValid)
        {
            return cloudEvent;
        }

        var missing = cloudEvent.SpecVersion.RequiredAttributes.Where(attr => cloudEvent[attr] is null).ToList();
        var joinedMissing = string.Join(", ", missing);
        throw new ArgumentException($"CloudEvent is missing required attributes: {joinedMissing}", nameof(message));
    }
    
    /// <summary>
    /// Indicates whether this <see cref="Message"/> holds a single CloudEvent.
    /// </summary>
    /// <param name="message">The message to check for the presence of a CloudEvent. Must not be null.</param>
    /// <returns>true, if the request is a CloudEvent</returns>
    public static bool IsCloudEvent(this Message message)
    {
        if (message == null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        return MimeUtilities.IsCloudEventsContentType(message.ContentType)
               || message.ApplicationProperties.ContainsKey(SpecVersionAmqpHeaderWithUnderscore)
               || message.ApplicationProperties.ContainsKey(SpecVersionAmqpHeaderWithColon);
    }

    private static bool TryGetAttributeName(string key, out string attributeName)
    {
        if (key.StartsWith(AmqpHeaderColonPrefix))
        {
            attributeName = key.Substring(AmqpHeaderColonPrefix.Length).ToLowerInvariant();
            return true;
        }

        if (key.StartsWith(AmqpHeaderUnderscorePrefix))
        {
            attributeName = key.Substring(AmqpHeaderUnderscorePrefix.Length).ToLowerInvariant();
            return true;
        }

        attributeName = null;
        return false;
    }
}