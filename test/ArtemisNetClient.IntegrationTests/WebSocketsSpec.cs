using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace ActiveMQ.Artemis.Client.IntegrationTests;

public class WebSocketsSpec : ActiveMQNetIntegrationSpec
{
    public WebSocketsSpec(ITestOutputHelper output) : base(output)
    {
    }
    
    [Fact]
    public async Task Should_send_and_receive_message_with_web_socket_endpoint()
    {
        string userName = Environment.GetEnvironmentVariable("ARTEMIS_USERNAME") ?? "artemis";
        string password = Environment.GetEnvironmentVariable("ARTEMIS_PASSWORD") ?? "artemis";
        string host = Environment.GetEnvironmentVariable("ARTEMIS_HOST") ?? "localhost";
        int port = int.Parse(Environment.GetEnvironmentVariable("ARTEMIS_WS_PORT") ?? "80");

        var endpoint = Endpoint.Create(host: host, port: port, user: userName, password: password, Scheme.Ws);
        
        var address = Guid.NewGuid().ToString();

        var connectionFactory = new ConnectionFactory();
        await using var connection = await connectionFactory.CreateAsync(endpoint, CancellationToken);
        await using var consumer = await connection.CreateConsumerAsync(address, RoutingType.Anycast);
        await using var producer = await connection.CreateProducerAsync(address, RoutingType.Anycast);
        
        await producer.SendAsync(new Message("msg"));

        var msg = await consumer.ReceiveAsync();
        await consumer.AcceptAsync(msg);
        
        Assert.Equal("msg", msg.GetBody<string>());
    }
}