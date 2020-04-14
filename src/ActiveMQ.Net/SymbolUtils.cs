using Amqp.Types;

namespace ActiveMQ.Net
{
    internal class SymbolUtils
    {
        public static readonly Symbol RoutingType = new Symbol("x-opt-routing-type");
    }
}