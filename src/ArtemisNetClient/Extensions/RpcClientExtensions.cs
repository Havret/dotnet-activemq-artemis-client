using System.Threading;
using System.Threading.Tasks;

namespace ActiveMQ.Artemis.Client;

public static class RpcClientExtensions
{
    static Task<Message> SendAsync(this IRpcClient rpcClient, string address, RoutingType? routingType, Message message, CancellationToken cancellationToken)
    {
        return rpcClient.SendAsync(address, null, message, cancellationToken);
    }
}