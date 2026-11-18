using System;

namespace ActiveMQ.Artemis.Client.Exceptions;

public class RequestReplyClientClosedException : ActiveMQArtemisClientException
{
    public RequestReplyClientClosedException() : base("The Request Reply Client was closed.")
    {
    }
    
    public RequestReplyClientClosedException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public RequestReplyClientClosedException(Exception innerException) : base("The Request Reply Client was closed.", innerException)
    {
    }

    public RequestReplyClientClosedException(string message, string errorCode, Exception innerException) : base(message, errorCode, innerException)
    {
    }

    /// <summary>
    /// True when the client was closed after the request had been sent, i.e. it may have been processed.
    /// </summary>
    internal bool RequestSent { get; set; }
}