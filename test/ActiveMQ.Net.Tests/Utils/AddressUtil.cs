using System;

namespace ActiveMQ.Net.Tests.Utils
{
    public static class AddressUtil
    {
        private static readonly Random PortGenerator = new Random();

        public static string GetAddress()
        {
            var port = PortGenerator.Next(10000, 65535);
            return $"amqp: //guest:guest@localhost:{port}";
        }
    }
}