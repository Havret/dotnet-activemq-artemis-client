using System.Text.RegularExpressions;
using ActiveMQ.Artemis.Client.Testing.Listener;
using Amqp;
using Amqp.Framing;
using Amqp.Listener;
using Amqp.Transactions;
using Amqp.Types;

namespace ActiveMQ.Artemis.Client.Testing;

internal class TestLinkProcessor : ILinkProcessor
{
    private readonly Action<Message> _onMessage;
    private readonly Action<MessageSourceInfo, MessageSource> _onMessageSource;

    public TestLinkProcessor(Action<Message> onMessage, Action<MessageSourceInfo, MessageSource> onMessageSource)
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

            var messageSourceInfo = GetMessageSourceInfo(source, attachContext.Link);
            var messageSource = new MessageSource();
            _onMessageSource(messageSourceInfo, messageSource);
            attachContext.Link.InitializeLinkEndpoint(new SourceLinkEndpoint(messageSource, attachContext.Link), 0);
            
            // override OnDispose so it won't throw NRE when message is null
            attachContext.Link.SetOnDispose((_, _, _, _) => { });
            
            attachContext.Link.CompleteAttach(attachContext.Attach, null);
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
    
    private static MessageSourceInfo GetMessageSourceInfo(Source source, ILink link)
    {
        var filterExpression = GetFilterExpression(source);

        // FQQN match
        if (Regex.Match(source.Address, "(.+)::(.+)") is { Success: true, Groups: { Count: 3 } groups })
        {
            return new MessageSourceInfo( groups[1].Value,  groups[2].Value, FilterExpression: filterExpression);
        }

        return new MessageSourceInfo(source.Address, link.Name, FilterExpression: filterExpression);
    }

    private static readonly Symbol _selectorFilterSymbol = new("apache.org:selector-filter:string");
    private static readonly Symbol _jmsSelectorSymbol = new("jms-selector");
    
    private static string? GetFilterExpression(Source source)
    {
        if (source.FilterSet is { } filterSet &&
            TryGetValue(filterSet, out var filterExpressionObj) &&
            filterExpressionObj is DescribedValue { Value: string filterExpression })
        {
            return filterExpression;
        }

        return null;
        
        static bool TryGetValue(Map filterSet, out object filterExpressionObj)
        {
            if (filterSet.TryGetValue(_selectorFilterSymbol, out filterExpressionObj))
            {
                return true;
            }

            // ReSharper disable once ConvertIfStatementToReturnStatement
            if (filterSet.TryGetValue(_jmsSelectorSymbol, out filterExpressionObj))
            {
                return true;
            }

            return false;
        }
    }
}