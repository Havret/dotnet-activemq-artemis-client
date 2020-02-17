namespace ActiveMQ.Net
{
    public class Message
    {
        private Properties _properties;
        internal Amqp.Message InnerMessage { get; }

        internal Message(Amqp.Message message)
        {
            InnerMessage = message;
        }

        public Message(string body)
        {
            InnerMessage = new Amqp.Message(body);
        }

        public Properties Properties => _properties ??= new Properties(InnerMessage);

        public T GetBody<T>()
        {
            if (InnerMessage.Body is T body)
                return body;

            return default;
        }
    }
}