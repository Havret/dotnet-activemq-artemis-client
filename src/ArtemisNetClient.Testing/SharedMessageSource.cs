using ActiveMQ.Artemis.Client.Testing.Utils;

namespace ActiveMQ.Artemis.Client.Testing;

internal class SharedMessageSource
{
    private readonly List<MessageSource> _messageSources = new();
    private int _cursor;

    public SharedMessageSource(MessageSource messageSource, string queue)
    {
        AddMessageSource(messageSource);
        Queue = queue;
    }
    
    public string Queue { get; }
    
    public void AddMessageSource(MessageSource messageSource)
    {
        _messageSources.Add(messageSource);
    }
    
    public void Enqueue(Message message)
    {
        if (message.GroupId is { Length: > 0 } groupId)
        {
            var cursor = ArtemisBucketHelper.GetBucket(groupId, _messageSources.Count);
            var messageSource = _messageSources[cursor];
            messageSource.Enqueue(message);
        }
        else
        {
            var messageSource = _messageSources[_cursor];
            messageSource.Enqueue(message);
            _cursor++;
        
            // If we have reached the end of the message source list, reset cursor to 0
            if (_cursor > _messageSources.Count - 1)
            {
                _cursor = 0;
            }
        }
    }
}