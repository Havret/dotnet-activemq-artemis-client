using Amqp.Listener;

namespace ActiveMQ.Artemis.Client.Testing;

internal class MessageProcessor : IMessageProcessor
{
    private readonly Action<Message> _onMessage;

    public MessageProcessor(Action<Message> onMessage)
    {
        _onMessage = onMessage;
    }

    public void Process(MessageContext messageContext)
    {
        messageContext.Complete();
        _onMessage(new Message(messageContext.Message));
    }

    public int Credit => 30;
}