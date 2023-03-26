using System.Reflection;
using ActiveMQ.Artemis.Client.Testing.Utils;
using Amqp;
using Amqp.Framing;
using Amqp.Listener;

namespace ActiveMQ.Artemis.Client.Testing.Listener;

internal static class Extensions
{
    private static bool IsDetaching(this Link link)
    {
        return link.LinkState >= LinkState.DetachPipe;
    }

    public static void AddSafeClosed(this Link link, ClosedCallback onLinkClosed)
    {
        link.AddClosedCallback(onLinkClosed);
        if (link.IsDetaching())
        {
            onLinkClosed(link, link.Error);
        }
    }

    public static void InitializeLinkEndpoint(this ListenerLink link, LinkEndpoint linkEndpoint, uint credit)
    {
        var type = typeof(TargetLinkEndpoint);
        var methodInfo = type.GetMethod("InitializeLinkEndpoint")!;
        methodInfo.Invoke(link, new object[] {linkEndpoint, credit});
    }

    public static void Dispose(this MessageContext messageContext, DeliveryState deliveryState)
    {
        var type = typeof(Context);
        var methodInfo = type.GetMethod("Dispose", BindingFlags.Instance | BindingFlags.NonPublic);
        methodInfo!.Invoke(messageContext, new object[] {deliveryState});
    }

    public static void SetSettleOnSend(this ListenerLink link, bool b)
    {
        ReflectionUtils.SetPropValue(link, "SettleOnSend", true);
    }

    public static string GetRemoteContainerId(this ListenerConnection connection)
    {
        return ReflectionUtils.GetPropValue<string>(connection, "RemoteContainerId")!;
    }

    public static ushort GetChannel(this ListenerSession session)
    {
        return ReflectionUtils.GetPropValue<ushort>(session, "Channel");
    }
}