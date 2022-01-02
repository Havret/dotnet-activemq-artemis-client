using Amqp.Framing;
using Amqp.Listener;
using Amqp.Transactions;
using Amqp.Types;

namespace ActiveMQ.Artemis.Client.Testing;

internal class TestLinkProcessor : ILinkProcessor
{
    private readonly Action<Message> _onMessage;
    private readonly Action<string, MessageSource> _onMessageSource;

    public TestLinkProcessor(Action<Message> onMessage, Action<string, MessageSource> onMessageSource)
    {
        _onMessage = onMessage;
        _onMessageSource = onMessageSource;
    }

    public void Process(AttachContext attachContext)
    {
        if (attachContext.Attach.Role && attachContext.Attach.Source is Source source)
        {
            if (source.Capabilities?.Contains(new Symbol("shared")) == true)
            {
                attachContext.Attach.OfferedCapabilities = new[] {new Symbol("SHARED-SUBS")};
            }

            var messageSource = new MessageSource();
            attachContext.Complete(new SourceLinkEndpoint(messageSource, attachContext.Link), 0);
            _onMessageSource(source.Address, messageSource);
        }
        else if (attachContext.Attach.Target is Target)
        {
            var messageProcessor = new MessageProcessor(_onMessage);
            attachContext.Complete(new TargetLinkEndpoint(messageProcessor, attachContext.Link), 30);
        }
        else if (attachContext.Attach.Target is Coordinator)
        {
            var transactionProcessor = new TransactionProcessor();
            attachContext.Complete(new TargetLinkEndpoint(transactionProcessor, attachContext.Link), 30);
        }
    }
}