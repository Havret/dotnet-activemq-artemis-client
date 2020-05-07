using Amqp.Types;

namespace ActiveMQ.Artemis.Client.Builders
{
    internal class FilterExpression : DescribedValue
    {
        private static readonly ulong _filterExpressionCode = 0x0000468C00000004L;
        public static readonly Symbol FilterExpressionName = new Symbol("apache.org:selector-filter:string");

        public FilterExpression(string filterExpression) : base(_filterExpressionCode, filterExpression)
        {
        }
    }
}