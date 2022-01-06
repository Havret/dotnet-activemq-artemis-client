using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Extensions.DependencyInjection;
using ActiveMQ.Artemis.Client;
using ActiveMQ.Artemis.Client.Extensions.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddActiveMqHostedService();

var endpoint = Endpoint.Create(
    host: "localhost",
    port: 5699,
    "artemis",
    "artemis"
);
var activeMqBuilder = builder.Services.AddActiveMq(name: "my-artemis", endpoints: new[] {endpoint});
activeMqBuilder.AddConsumer(address: "foo", routingType: RoutingType.Anycast,
    handler: async (message, consumer, serviceProvider, cancellationToken) =>
    {
        var body = message.GetBody<string>();

        var bar = body + "-" + "bar";

        var producer = serviceProvider.GetRequiredService<MyProducer>();
        await producer.Publish(bar, cancellationToken);

        await consumer.AcceptAsync(message);
    });
activeMqBuilder.AddProducer<MyProducer>("bar", RoutingType.Multicast);


var app = builder.Build();

app.Run();

public partial class Program { }

public class MyProducer
{
    private readonly IProducer _producer;

    public MyProducer(IProducer producer) => _producer = producer;

    public async Task Publish(string payload, CancellationToken cancellationToken)
    {
        var message = new Message(payload);
        await _producer.SendAsync(message, cancellationToken);
    }
}