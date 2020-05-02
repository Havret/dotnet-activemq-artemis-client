using Amqp.Types;

namespace ActiveMQ.Net.Builders
{
    internal class NoLocalFilter : DescribedValue
    {
        private static readonly ulong _descriptor = 0x0000468C00000003L;
        private static readonly string _described = "NoLocalFilter{}";
        public static readonly Symbol NoLocalFilterName = new Symbol("apache.org:no-local-filter:list");

        private NoLocalFilter() : base(_descriptor, _described)
        {
        }

        public static NoLocalFilter Instance { get; } = new NoLocalFilter();
    }
}