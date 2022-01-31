using System.Threading;
using System.Threading.Tasks;

namespace ActiveMQ.Artemis.Client;

public static class RequestReplyClientExtensions
{
    public static Task<Message> SendAsync(this IRequestReplyClient requestReplyClient, string address, Message message, CancellationToken cancellationToken)
    {
        return requestReplyClient.SendAsync(address, null, message, cancellationToken);
    }
}