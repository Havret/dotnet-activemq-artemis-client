using System;
using System.Threading;
using System.Threading.Tasks;

namespace ActiveMQ.Artemis.Client;

public interface IRequestReplyClient : IAsyncDisposable
{
    Task<Message> SendAsync(string address, RoutingType? routingType, Message message, CancellationToken cancellationToken);
}