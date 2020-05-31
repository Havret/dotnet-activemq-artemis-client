namespace ActiveMQ.Artemis.Client
{
    public sealed class ApplicationProperties
    {
        private readonly Amqp.Framing.ApplicationProperties _innerProperties;

        public ApplicationProperties(Amqp.Message innerMessage) 
        {
            _innerProperties = innerMessage.ApplicationProperties ??= new Amqp.Framing.ApplicationProperties();
        }

        public object this[string key]
        {
            get => _innerProperties.Map[key];
            set => _innerProperties.Map[key] = value;
        }

        internal bool TryGetValue<T>(string key, out T value)
        {
            if (_innerProperties.Map.TryGetValue(key, out var objectValue) && objectValue is T typedValue)
            {
                value = typedValue;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }
    }
}