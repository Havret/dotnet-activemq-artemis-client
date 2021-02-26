using Amqp.Types;

namespace ActiveMQ.Artemis.Client
{
    internal class SymbolUtils
    {
        public static readonly Symbol RoutingType = new Symbol("x-opt-routing-type");
    }
}