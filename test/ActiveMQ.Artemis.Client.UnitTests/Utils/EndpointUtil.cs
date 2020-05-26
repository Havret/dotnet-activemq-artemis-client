using System;

namespace ActiveMQ.Artemis.Client.UnitTests.Utils
{
    public static class EndpointUtil
    {
        private static int _port = 10000;
        private static readonly object _syncRoot = new object();

        public static Endpoint GetUniqueEndpoint()
        {
            lock (_syncRoot)
            {
                var random = new Random();
                _port += random.Next(1, 10);
                return Endpoint.Create("localhost", _port, "guest", "guest");
            }
        }
    }
}