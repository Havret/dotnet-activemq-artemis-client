using System;

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
    }
}