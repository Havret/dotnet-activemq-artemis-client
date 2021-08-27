using System.Net;
using System.Net.Sockets;

namespace ActiveMQ.Artemis.Client.UnitTests.Utils
{
    public static class EndpointUtil
    {
        public static Endpoint GetUniqueEndpoint()
        {
            var port = GetFreePort();
            return Endpoint.Create("localhost", port, "guest", "guest");
        }

        private static int GetFreePort()
        {
            var tcpListener = new TcpListener(IPAddress.Loopback, 0);
            tcpListener.Start();
            var port = ((IPEndPoint)tcpListener.LocalEndpoint).Port;
            tcpListener.Stop();
            return port;
        }
    }
}