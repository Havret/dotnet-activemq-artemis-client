using Amqp.Types;

namespace ActiveMQ.Artemis.Client
{
    internal sealed class MessageAnnotations
    {
        private static readonly Symbol _scheduledDeliveryTime = new Symbol("x-opt-delivery-time");
        private static readonly Symbol _scheduledDeliveryDelay = new Symbol("x-opt-delivery-delay");

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

        public long? ScheduledDeliveryTime
        {
            get
            {
                if (_innerProperties.Map.TryGetValue(_scheduledDeliveryTime, out var value))
                {
                    if (value is long scheduledDeliveryTime)
                    {
                        return scheduledDeliveryTime;
                    }
                }

                return null;
            }
            set
            {
                if (value != default)
                {
                    _innerProperties.Map[_scheduledDeliveryTime] = value;
                }
                else
                {
                    _innerProperties.Map.Remove(_scheduledDeliveryTime);
                }
            }
        }
        
        public long? ScheduledDeliveryDelay
        {
            get
            {
                if (_innerProperties.Map.TryGetValue(_scheduledDeliveryDelay, out var value))
                {
                    if (value is long scheduledDeliveryDelay)
                    {
                        return scheduledDeliveryDelay;
                    }
                }

                return null;
            }
            set
            {
                if (value != default)
                {
                    _innerProperties.Map[_scheduledDeliveryDelay] = value;
                }
                else
                {
                    _innerProperties.Map.Remove(_scheduledDeliveryDelay);
                }
            }
        }
    }
}