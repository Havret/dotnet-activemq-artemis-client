using System.Reflection;
using Amqp.Framing;
using Amqp.Listener;

namespace ActiveMQ.Artemis.Client.Testing.Listener;

internal static class Extensions
{
    public static void Dispose(this MessageContext messageContext, DeliveryState deliveryState)
    {
        var type = typeof(Context);
        var methodInfo = type.GetMethod("Dispose", BindingFlags.Instance | BindingFlags.NonPublic);
        methodInfo!.Invoke(messageContext, new object[] {deliveryState});
    }
}