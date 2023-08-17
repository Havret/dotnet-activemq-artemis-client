using System.Data;
using System.Text.RegularExpressions;
using ActiveMQ.Artemis.Client.InternalUtilities;
using ActiveMQ.Artemis.Client.Testing.Utils;

namespace ActiveMQ.Artemis.Client.Testing;

internal class SharedMessageSource
{
    private readonly List<MessageSource> _messageSources = new();
    private int _cursor;

    public SharedMessageSource(MessageSourceInfo info, MessageSource messageSource)
    {
        AddMessageSource(messageSource);
        Info = info;
    }
    
    public MessageSourceInfo Info { get; }
    
    public void AddMessageSource(MessageSource messageSource)
    {
        _messageSources.Add(messageSource);
    }
    
    public void Enqueue(Message message)
    {
        if (Info.FilterExpression != null && FilterExpressionMatches(message) == false)
        {
            return;
        }

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

    private bool FilterExpressionMatches(Message message)
    {
        var dataTable = new DataTable();
        var dataRow = dataTable.NewRow();
        foreach (var key in message.ApplicationProperties.Keys)
        {
            var value = message.ApplicationProperties[key];
            if (value != null)
            {
                dataTable.Columns.Add(key, value.GetType());
                dataRow[key] = value;
            }
        }

        if (message.Priority is { } priority)
        {
            var columnName = "AMQPriority";
            dataTable.Columns.Add(columnName, priority.GetType());
            dataRow[columnName] = priority;
        }

        if (message.TimeToLive is { } timeToLive)
        {
            var columnName = "AMQExpiration";
            var value = DateTimeOffset.UtcNow.Add(timeToLive).ToUnixTimeMilliseconds();
            dataTable.Columns.Add(columnName, value.GetType());
            dataRow[columnName] = value;
        }

        if (message.DurabilityMode is { } durabilityMode)
        {
            var columnName = "AMQDurable";
            dataTable.Columns.Add(columnName, typeof(string));
            dataRow[columnName] = durabilityMode == DurabilityMode.Durable ? "DURABLE" : "NON_DURABLE";
        }

        if (message.CreationTime is { } creationTime)
        {
            var columnName = "AMQTimestamp";
            var value = DateTime.SpecifyKind(creationTime, DateTimeKind.Utc).ToUnixTimeMilliseconds();
            dataTable.Columns.Add(columnName, value.GetType());
            dataRow[columnName] = value;
        }

        dataTable.Rows.Add(dataRow);
        
        while (true)
        {
            try
            {
                return dataTable.Select(Info.FilterExpression).Any();
            }
            catch (EvaluateException e) when(e.Message.StartsWith("Cannot find column"))
            {
                var match = Regex.Match(e.Message, @"Cannot find column \[(\w+)\]\.");
                if (match.Success)
                {
                    dataTable.Columns.Add(match.Groups[1].Value);
                }
                else
                {
                    throw;
                }
            }
        }
    }
}