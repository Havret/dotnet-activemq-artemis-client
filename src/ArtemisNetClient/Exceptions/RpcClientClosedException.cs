using System;

namespace ActiveMQ.Artemis.Client.Exceptions;

public class RpcClientClosedException : ActiveMQArtemisClientException
{
    public RpcClientClosedException() : base("The RpcClient was closed.")
    {
    }
    
    public RpcClientClosedException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public RpcClientClosedException(Exception innerException) : base("The RpcClient was closed.", innerException)
    {
    }

    public RpcClientClosedException(string message, string errorCode, Exception innerException) : base(message, errorCode, innerException)
    {
    }
}