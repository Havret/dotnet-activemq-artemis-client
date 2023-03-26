using System.Text.RegularExpressions;
using Amqp.Framing;
using Amqp.Listener;
using Amqp.Transactions;
using Amqp.Types;

namespace ActiveMQ.Artemis.Client.Testing;

internal class TestLinkProcessor : ILinkProcessor
{
    private readonly Action<Message> _onMessage;
    private readonly Action<string, string, MessageSource> _onMessageSource;

    public TestLinkProcessor(Action<Message> onMessage, Action<string, string, MessageSource> onMessageSource)
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
                attachContext.Attach.OfferedCapabilities = new[] { new Symbol("SHARED-SUBS") };
            }

            var (address, queue) = ParseAddressAndQueue(source, attachContext.Link);

            var messageSource = new MessageSource();
            _onMessageSource(address, queue, messageSource);
            attachContext.Complete(new SourceLinkEndpoint(messageSource, attachContext.Link), 0);
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
    
    private (string address, string queue) ParseAddressAndQueue(Source input, ListenerLink listenerLink)
    {
        // FQQN match
        if (Regex.Match(input.Address, "(.+)::(.+)") is { Success: true, Groups: { Count: 3 } groups })
        {
            return (address: groups[1].Value, queue: groups[2].Value);
        }

        return (address: input.Address, queue: listenerLink.Name);
    }
}