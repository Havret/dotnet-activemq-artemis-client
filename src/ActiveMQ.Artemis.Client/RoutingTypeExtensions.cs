using System;
using Amqp.Types;

namespace ActiveMQ.Artemis.Client
{
    internal static class RoutingTypeExtensions
    {
        private static readonly object _addressRoutingTypeAnycast = (byte) 1;
        private static readonly object _addressRoutingTypeMulticast = (byte) 0;
        private static readonly object _addressRoutingTypeBoth = null;

        public static Symbol GetRoutingCapability(this QueueRoutingType routingType) => routingType switch
        {
            QueueRoutingType.Anycast => RoutingCapabilities.Anycast,
            QueueRoutingType.Multicast => RoutingCapabilities.Multicast,
            _ => throw new ArgumentOutOfRangeException(nameof(routingType), $"RoutingType {routingType.ToString()} is not supported.")
        };

        public static Symbol[] GetRoutingCapabilities(this AddressRoutingType routingType) => routingType switch
        {
            AddressRoutingType.Anycast => new[] { RoutingCapabilities.Anycast },
            AddressRoutingType.Multicast => new[] { RoutingCapabilities.Multicast },
            AddressRoutingType.Both => null,
            _ => throw new ArgumentOutOfRangeException(nameof(routingType), $"RoutingType {routingType.ToString()} is not supported.")
        };

        public static object GetRoutingAnnotation(this AddressRoutingType routingType) => routingType switch
        {
            AddressRoutingType.Anycast => _addressRoutingTypeAnycast,
            AddressRoutingType.Multicast => _addressRoutingTypeMulticast,
            AddressRoutingType.Both => _addressRoutingTypeBoth,
            _ => throw new ArgumentOutOfRangeException(nameof(routingType), $"RoutingType {routingType.ToString()} is not supported.")
        };
    }
}