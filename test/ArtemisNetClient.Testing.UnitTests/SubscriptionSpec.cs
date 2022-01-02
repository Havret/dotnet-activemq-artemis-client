using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.TestUtils;
using Xunit;

namespace ActiveMQ.Artemis.Client.Testing.UnitTests;

public class SubscriptionSpec
{
    [Fact]
    public async Task Should_subscribe_on_a_given_address()
    {
        var endpoint = EndpointUtil.GetUniqueEndpoint();
        using var testKit = new TestKit(endpoint);
        var testAddress = "test_address";

        using var subscription = testKit.Subscribe(testAddress);

        var connectionFactory = new ConnectionFactory();
        await using var connection = await connectionFactory.CreateAsync(endpoint);
        await using var producer = await connection.CreateProducerAsync(testAddress);

        await producer.SendAsync(new Message("foo"));

        var message = await subscription.ReceiveAsync();
        Assert.Equal("foo", message.GetBody<string>());
    }
}