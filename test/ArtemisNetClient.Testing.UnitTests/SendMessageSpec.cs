using System;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.TestUtils;
using ActiveMQ.Artemis.Client.Transactions;
using Xunit;

namespace ActiveMQ.Artemis.Client.Testing.UnitTests;

public class SendMessageSpec
{
    [Fact]
    public async Task Should_send_message_to_given_address()
    {
        var endpoint = EndpointUtil.GetUniqueEndpoint();
        using var testKit = new TestKit(endpoint);

        var connectionFactory = new ConnectionFactory();
        await using var connection = await connectionFactory.CreateAsync(endpoint);
        
        var testAddress = "test_address";
        await using var consumer = await connection.CreateConsumerAsync(testAddress, RoutingType.Anycast);

        await testKit.SendMessageAsync(testAddress, new Message("foo"));

        var message = await consumer.ReceiveAsync();
        Assert.Equal("foo", message.GetBody<string>());
    }

    [Fact]
    public async Task Should_send_message_when_client_is_using_transactions()
    {
        var endpoint = EndpointUtil.GetUniqueEndpoint();
        using var testKit = new TestKit(endpoint);

        var connectionFactory = new ConnectionFactory();
        await using var connection = await connectionFactory.CreateAsync(endpoint);
        
        var testAddress = "test_address";
        await using var consumer = await connection.CreateConsumerAsync(testAddress, RoutingType.Anycast);

        await testKit.SendMessageAsync(testAddress, new Message("foo"));

        var message = await consumer.ReceiveAsync();
        Assert.Equal("foo", message.GetBody<string>());

        await using var transaction = new Transaction();
        await consumer.AcceptAsync(message, transaction);
        await transaction.CommitAsync();
    }

    [Fact]
    public async Task Should_send_message_to_shared_consumer()
    {
        var endpoint = EndpointUtil.GetUniqueEndpoint();
        using var testKit = new TestKit(endpoint);

        var connectionFactory = new ConnectionFactory();
        await using var connection = await connectionFactory.CreateAsync(endpoint);
        
        var testAddress = "test_address";
        await using var consumer = await connection.CreateConsumerAsync(new ConsumerConfiguration
        {
            Address = testAddress,
            Queue = Guid.NewGuid().ToString(),
            Shared = true
        });

        await testKit.SendMessageAsync(testAddress, new Message("foo"));

        var message = await consumer.ReceiveAsync();
        Assert.Equal("foo", message.GetBody<string>());
    }
    
    [Fact]
    public async Task Should_send_message_to_shared_durable_consumer()
    {
        var endpoint = EndpointUtil.GetUniqueEndpoint();
        using var testKit = new TestKit(endpoint);

        var connectionFactory = new ConnectionFactory();
        await using var connection = await connectionFactory.CreateAsync(endpoint);
        
        var testAddress = "test_address";
        await using var consumer = await connection.CreateConsumerAsync(new ConsumerConfiguration
        {
            Address = testAddress,
            Queue = Guid.NewGuid().ToString(),
            Shared = true,
            Durable = true
        });

        await testKit.SendMessageAsync(testAddress, new Message("foo"));

        var message = await consumer.ReceiveAsync();
        Assert.Equal("foo", message.GetBody<string>());
    }
    
    [Fact]
    public async Task Should_send_message_to_consumer_attached_via_FQQN()
    {
        var endpoint = EndpointUtil.GetUniqueEndpoint();
        using var testKit = new TestKit(endpoint);

        var connectionFactory = new ConnectionFactory();
        await using var connection = await connectionFactory.CreateAsync(endpoint);
        
        var testAddress = "test_address";
        await using var consumer = await connection.CreateConsumerAsync(address: testAddress, queue: Guid.NewGuid().ToString());

        await testKit.SendMessageAsync(testAddress, new Message("foo"));

        var message = await consumer.ReceiveAsync();
        Assert.Equal("foo", message.GetBody<string>());
    }
    
}