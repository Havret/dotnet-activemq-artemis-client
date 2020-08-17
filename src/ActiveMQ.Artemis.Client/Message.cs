using System;
using ActiveMQ.Artemis.Client.InternalUtilities;
using Amqp.Framing;
using Amqp.Types;

namespace ActiveMQ.Artemis.Client
{
    public class Message
    {
        private Header _header;
        private Properties _properties;
        private ApplicationProperties _applicationProperties;
        private MessageAnnotations _messageAnnotations;

        internal Message(Amqp.Message message)
        {
            InnerMessage = message;
        }

        public Message(object body)
        {
            InnerMessage = new Amqp.Message
            {
                BodySection = GetBodySection(body)
            };
        }

        private static RestrictedDescribed GetBodySection(object body)
        {
            switch (body)
            {
                case string _:
                case char _:
                case byte _:
                case sbyte _:
                case short _:
                case ushort _:
                case int _:
                case uint _:
                case long _:
                case ulong _:
                case float _:
                case double _:
                case Guid _:
                case DateTime _:
                    return new AmqpValue { Value = body };
                case byte[] payload:
                    return new Data { Binary = payload };
                case List list:
                    return new AmqpSequence { List = list };
                case null:
                    throw new ArgumentNullException(nameof(body));
                default:
                    throw new ArgumentOutOfRangeException(nameof(body), $"The type '{body.GetType().FullName}' is not a valid AMQP type and cannot be encoded.");
            }
        }

        internal Amqp.Message InnerMessage { get; }

        private Header Header => _header ??= new Header(InnerMessage);

        internal Properties Properties => _properties ??= new Properties(InnerMessage);

        internal MessageAnnotations MessageAnnotations => _messageAnnotations ??= new MessageAnnotations(InnerMessage);
        public ApplicationProperties ApplicationProperties => _applicationProperties ??= new ApplicationProperties(InnerMessage);

        public string MessageId
        {
            get => Properties.MessageId;
            set => Properties.MessageId = value;
        }

        public T GetMessageId<T>()
        {
            if (Properties.ObjectMessageId is T messageId)
            {
                return messageId;
            }

            return default;
        }

        public void SetMessageId<T>(T messageId)
        {
            Properties.ObjectMessageId = messageId;
        }

        public string CorrelationId
        {
            get => Properties.CorrelationId;
            set => Properties.CorrelationId = value;
        }
        
        public T GetCorrelationId<T>()
        {
            if (Properties.ObjectCorrelationId is T correlationId)
            {
                return correlationId;
            }

            return default;
        }
        
        public void SetCorrelationId<T>(T correlationId)
        {
            Properties.ObjectCorrelationId = correlationId;
        }

        /// <summary>
        /// The subject of the message. A common field for summary information about the message content and purpose.
        /// </summary>
        public string Subject
        {
            get => Properties.Subject;
            set => Properties.Subject = value;
        }

        /// <summary>
        /// The address of the node the message is destined for.
        /// </summary>
        public string To => Properties.To;

        /// <summary>
        /// The address of the node to send replies to.
        /// </summary>
        public string ReplyTo
        {
            get => Properties.ReplyTo;
            set => Properties.ReplyTo = value;
        }

        /// <summary>
        /// The identity of the user responsible for producing the message.
        /// The client sets this value, and it MAY be authenticated by intermediaries.
        /// </summary>
        public byte[] UserId
        {
            get => Properties.UserId;
            set => Properties.UserId = value;
        }

        /// <summary>
        /// The number of unsuccessful previous attempts to deliver this message.
        /// If this value is non-zero it can be taken as an indication that the delivery might be a duplicate.
        /// On first delivery, the value is zero.
        /// </summary>
        public uint DeliveryCount => Header.DeliveryCount;

        public DurabilityMode? DurabilityMode
        {
            get => Header.Durable switch
            {
                true => Client.DurabilityMode.Durable,
                false => Client.DurabilityMode.Nondurable,
                null => null
            };
            set => Header.Durable = value switch
            {
                Client.DurabilityMode.Durable => true,
                Client.DurabilityMode.Nondurable => false,
                null => null,
                _ => throw new ArgumentOutOfRangeException(nameof(value))
            };
        }

        public byte? Priority
        {
            get => Header.Priority;
            set
            {
                if (value > 9) throw new ArgumentOutOfRangeException(nameof(value), $"Priority value {value} is out of range (0..9).");
                Header.Priority = value;
            }
        }

        public string GroupId
        {
            get => Properties.GroupId;
            set => Properties.GroupId = value;
        }

        public uint? GroupSequence
        {
            get => Properties.GroupSequence;
            set => Properties.GroupSequence = value;
        }

        public TimeSpan? TimeToLive
        {
            get => Header.Ttl.HasValue ? TimeSpan.FromMilliseconds(Header.Ttl.Value) : default(TimeSpan?);
            set => Header.Ttl = value switch
            {
                { } timeSpan => Convert.ToUInt32(timeSpan.TotalMilliseconds),
                null => null
            };
        }

        public DateTime? CreationTime
        {
            get => Properties.CreationTime;
            set => Properties.CreationTime = value;
        }

        public DateTime? ScheduledDeliveryTime
        {
            get => MessageAnnotations.ScheduledDeliveryTime switch
            {
                { } scheduledDeliveryTime => DateTimeExtensions.FromUnixTimeMilliseconds(scheduledDeliveryTime),
                null => null
            };
            set => MessageAnnotations.ScheduledDeliveryTime = value switch
            {
                { } scheduledDeliveryTime => scheduledDeliveryTime.ToUnixTimeMilliseconds(),
                null => null
            };
        }
        
        public TimeSpan? ScheduledDeliveryDelay
        {
            get => MessageAnnotations.ScheduledDeliveryDelay switch
            {
                { } scheduledDeliveryDelay => TimeSpan.FromMilliseconds(scheduledDeliveryDelay),
                null => null
            };
            set => MessageAnnotations.ScheduledDeliveryDelay = value switch
            {
                { } scheduledDeliveryDelay => Convert.ToUInt32(scheduledDeliveryDelay.TotalMilliseconds),
                null => null
            };
        }

        public T GetBody<T>()
        {
            if (InnerMessage.Body is T body)
                return body;

            return default;
        }
    }
}