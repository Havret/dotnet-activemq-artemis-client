namespace ActiveMQ.Net
{
    public class Message
    {
        internal Amqp.Message InnerMessage { get; }

        internal Message(Amqp.Message message)
        {
            InnerMessage = message;
        }

        public Message(string body)
        {
            InnerMessage = new Amqp.Message(body);
        }

        public T GetBody<T>()
        {
            if (InnerMessage.Body is T body)
                return body;

            return default;
        }
    }
}