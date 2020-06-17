using System;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client;

namespace PingPong
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var endpoint = Endpoint.Create("localhost", 5672, "guest", "guest");
            await using var ping = await Ping.CreateAsync(endpoint);
            await using var pong = await Pong.CreateAsync(endpoint);
            var start = await ping.Start(skipMessages: 100, numberOfMessages: 10000);
            Console.WriteLine(start);
        }
    }
}