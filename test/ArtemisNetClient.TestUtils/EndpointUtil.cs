using System;
using System.Net;
using System.Net.Sockets;

namespace ActiveMQ.Artemis.Client.TestUtils
{
    public static class EndpointUtil
    {
        public static Endpoint GetEndpoint()
        {
            string userName = Environment.GetEnvironmentVariable("ARTEMIS_USERNAME") ?? "artemis";
            string password = Environment.GetEnvironmentVariable("ARTEMIS_PASSWORD") ?? "artemis";
            string host = Environment.GetEnvironmentVariable("ARTEMIS_HOST") ?? "localhost";
            int port = int.Parse(Environment.GetEnvironmentVariable("ARTEMIS_PORT") ?? "5672");
            return Endpoint.Create(host, port, userName, password);
        }
        
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