using System;
using Amqp.Framing;
using Amqp.Types;

namespace ActiveMQ.Net
{
    public class Message
    {
        private Properties _properties;
        private ApplicationProperties _applicationProperties;
        private MessageAnnotations _messageAnnotations;
        internal Amqp.Message InnerMessage { get; }

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

        public Properties Properties => _properties ??= new Properties(InnerMessage);

        public ApplicationProperties ApplicationProperties => _applicationProperties ??= new ApplicationProperties(InnerMessage);

        internal MessageAnnotations MessageAnnotations => _messageAnnotations ??= new MessageAnnotations(InnerMessage);

        public T GetBody<T>()
        {
            if (InnerMessage.Body is T body)
                return body;

            return default;
        }
    }
}