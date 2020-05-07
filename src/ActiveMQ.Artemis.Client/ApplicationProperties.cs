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
    }
}