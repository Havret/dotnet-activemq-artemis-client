using System.Threading;
using System.Threading.Tasks;

namespace ActiveMQ.Artemis.Client;

public static class RpcClientExtensions
{
    static Task<Message> SendAsync(this IRequestReplyClient requestReplyClient, string address, RoutingType? routingType, Message message, CancellationToken cancellationToken)
    {
        return requestReplyClient.SendAsync(address, null, message, cancellationToken);
    }
}