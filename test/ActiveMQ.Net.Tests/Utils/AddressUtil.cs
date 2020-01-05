using System.Threading;

namespace ActiveMQ.Net.Tests.Utils
{
    public static class AddressUtil
    {
        private static int _port = 10000;

        public static string GetUniqueAddress()
        {
            return $"amqp: //guest:guest@localhost:{Interlocked.Increment(ref _port)}";
        }
    }
}