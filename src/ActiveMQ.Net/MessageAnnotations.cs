using Amqp.Types;

namespace ActiveMQ.Net
{
    internal sealed class MessageAnnotations
    {
        private readonly Amqp.Framing.MessageAnnotations _innerProperties;

        public MessageAnnotations(Amqp.Message innerMessage) 
        {
            _innerProperties = innerMessage.MessageAnnotations ??= new Amqp.Framing.MessageAnnotations();
        }

        public object this[Symbol key]
        {
            get => _innerProperties.Map[key];
            set => _innerProperties.Map[key] = value;
        }
    }
}