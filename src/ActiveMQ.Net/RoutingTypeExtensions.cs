using System;
using Amqp.Types;

namespace ActiveMQ.Net
{
    internal static class RoutingTypeExtensions
    {
        public static Symbol GetRoutingCapability(this RoutingType routingType) => routingType switch
        {
            RoutingType.Anycast => RoutingCapabilities.Anycast,
            RoutingType.Multicast => RoutingCapabilities.Multicast,
            _ => throw new ArgumentOutOfRangeException(nameof(routingType), $"RoutingType {routingType.ToString()} is not supported.")
        };

        public static Symbol[] GetRoutingCapabilities(this RoutingType? routingType) => routingType switch
        {
            RoutingType.Anycast => new[] { RoutingCapabilities.Anycast },
            RoutingType.Multicast => new[] { RoutingCapabilities.Multicast },
            null => new[] { RoutingCapabilities.Anycast, RoutingCapabilities.Multicast },
            _ => throw new ArgumentOutOfRangeException(nameof(routingType), $"RoutingType {routingType.ToString()} is not supported.")
        };
        
    }
}