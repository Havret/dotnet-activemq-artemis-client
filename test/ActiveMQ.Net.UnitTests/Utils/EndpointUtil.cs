using System.Threading;

namespace ActiveMQ.Net.Tests.Utils
{
    public static class EndpointUtil
    {
        private static int _port = 10000;

        public static Endpoint GetUniqueEndpoint()
        {
            return Endpoint.Create("localhost", Interlocked.Increment(ref _port), "guest", "guest");
        }
    }
}