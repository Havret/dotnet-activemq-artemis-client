using Amqp.Framing;
using Amqp.Handler;

namespace ActiveMQ.Artemis.Client.Testing;

internal class Handler : IHandler
{
    public bool CanHandle(EventId id)
    {
        switch (id)
        {
            case EventId.LinkRemoteClose:
            case EventId.LinkLocalClose:
                return true;
            default:
                return false;
        }
    }

    public void Handle(Event protocolEvent)
    {
        if (protocolEvent.Id is EventId.LinkRemoteClose or EventId.LinkLocalClose)
        {
            if (protocolEvent.Context is Detach {Closed: true} detach)
            {
                detach.Closed = false;
            }
        }
    }
}