using System;
using Amqp.Types;

namespace ActiveMQ.Artemis.Client
{
    internal static class RoutingTypeExtensions
    {
        private static readonly object _addressRoutingTypeAnycast = (sbyte)1;
        private static readonly object _addressRoutingTypeMulticast = (sbyte) 0;
        private static readonly object _addressRoutingTypeBoth = null;

        public static Symbol GetRoutingCapability(this RoutingType routingType) => routingType switch
        {
            RoutingType.Anycast => RoutingCapabilities.Anycast,
            RoutingType.Multicast => RoutingCapabilities.Multicast,
            _ => throw new ArgumentOutOfRangeException(nameof(routingType), $"RoutingType {routingType.ToString()} is not supported.")
        };

        public static object GetRoutingAnnotation(this RoutingType? routingType) => routingType switch
        {
            RoutingType.Anycast => _addressRoutingTypeAnycast,
            RoutingType.Multicast => _addressRoutingTypeMulticast,
            null => _addressRoutingTypeBoth,
            _ => throw new ArgumentOutOfRangeException(nameof(routingType), $"RoutingType {routingType.ToString()} is not supported.")
        };
    }
}