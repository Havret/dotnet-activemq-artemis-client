using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.TestUtils;
using Xunit;

namespace ActiveMQ.Artemis.Client.Testing.UnitTests;

public class FilterExpressionsSpec
{
    [Fact]
    public async Task Should_filter_messages_based_on_priority()
    {
        var endpoint = EndpointUtil.GetUniqueEndpoint();
        using var testKit = new TestKit(endpoint);

        var connectionFactory = new ConnectionFactory();
        await using var connection = await connectionFactory.CreateAsync(endpoint);
        
        var address = Guid.NewGuid().ToString();
        await using var highPriorityConsumer = await connection.CreateConsumerAsync(new ConsumerConfiguration
        {
            Address = address,
            RoutingType = RoutingType.Anycast,
            FilterExpression = "AMQPriority = 9"
        });
        await using var lowPriorityConsumer = await connection.CreateConsumerAsync(new ConsumerConfiguration
        {
            Address = address,
            RoutingType = RoutingType.Anycast,
            FilterExpression = "AMQPriority = 0"
        });

        for (int i = 0; i < 3; i++)
        {
            await testKit.SendMessageAsync(address, new Message("highPriorityMsg") { Priority = 9 });
            await testKit.SendMessageAsync(address, new Message("lowPriorityMsg") { Priority = 0 });
        }

        var lowPriorityMessages = await ReceiveMessages(lowPriorityConsumer, 3);
        var highPriorityMessages = await ReceiveMessages(highPriorityConsumer, 3);

        Assert.All(lowPriorityMessages, x => Assert.Equal("lowPriorityMsg", x.GetBody<string>()));
        Assert.All(highPriorityMessages, x => Assert.Equal("highPriorityMsg", x.GetBody<string>()));
    }
    
    
    [Fact]
    public async Task Should_filter_messages_based_on_message_expiration_time()
    {
        var endpoint = EndpointUtil.GetUniqueEndpoint();
        using var testKit = new TestKit(endpoint);

        var connectionFactory = new ConnectionFactory();
        await using var connection = await connectionFactory.CreateAsync(endpoint);
        var address = Guid.NewGuid().ToString();

        var messageExpiryTime = DateTimeOffset.UtcNow.AddMilliseconds(100_000).ToUnixTimeMilliseconds();

        await using var shortExpiryConsumer = await connection.CreateConsumerAsync(new ConsumerConfiguration
        {
            Address = address,
            RoutingType = RoutingType.Anycast,
            FilterExpression = $"AMQExpiration < {messageExpiryTime}"
        });
        await using var longExpiryConsumer = await connection.CreateConsumerAsync(new ConsumerConfiguration
        {
            Address = address,
            RoutingType = RoutingType.Anycast,
            FilterExpression = $"AMQExpiration > {messageExpiryTime}"
        });

        for (int i = 0; i < 3; i++)
        {
            await testKit.SendMessageAsync(address, new Message("longExpiryMsg") { TimeToLive = TimeSpan.FromMilliseconds(200_000) });
            await testKit.SendMessageAsync(address, new Message("shortExpiryMsg") { TimeToLive = TimeSpan.FromMilliseconds(50_000) });
        }

        var longExpiryMessages = await ReceiveMessages(longExpiryConsumer, 3);
        var shortExpiryMessages = await ReceiveMessages(shortExpiryConsumer, 3);

        Assert.All(longExpiryMessages, x => Assert.Equal("longExpiryMsg", x.GetBody<string>()));
        Assert.All(shortExpiryMessages, x => Assert.Equal("shortExpiryMsg", x.GetBody<string>()));
    }
    
    [Fact]
    public async Task Should_filter_messages_based_on_message_durability()
    {
        var endpoint = EndpointUtil.GetUniqueEndpoint();
        using var testKit = new TestKit(endpoint);

        var connectionFactory = new ConnectionFactory();
        await using var connection = await connectionFactory.CreateAsync(endpoint);
        var address = Guid.NewGuid().ToString();

        await using var durableMessageConsumer = await connection.CreateConsumerAsync(new ConsumerConfiguration
        {
            Address = address,
            RoutingType = RoutingType.Anycast,
            FilterExpression = "AMQDurable = 'DURABLE'"
        });
        await using var nondurableConsumer = await connection.CreateConsumerAsync(new ConsumerConfiguration
        {
            Address = address,
            RoutingType = RoutingType.Anycast,
            FilterExpression = "AMQDurable = 'NON_DURABLE'"
        });

        for (int i = 0; i < 3; i++)
        {
            await testKit.SendMessageAsync(address, new Message("durableMsg") { DurabilityMode = DurabilityMode.Durable });
            await testKit.SendMessageAsync(address, new Message("nondurableMsg") { DurabilityMode = DurabilityMode.Nondurable });
        }

        var nondurableMessages = await ReceiveMessages(nondurableConsumer, 3);
        var durableMessages = await ReceiveMessages(durableMessageConsumer, 3);

        Assert.All(nondurableMessages, x => Assert.Equal("nondurableMsg", x.GetBody<string>()));
        Assert.All(durableMessages, x => Assert.Equal("durableMsg", x.GetBody<string>()));
    }
    
    [Fact]
    public async Task Should_filter_messages_based_on_message_creation_time()
    {
        var endpoint = EndpointUtil.GetUniqueEndpoint();
        using var testKit = new TestKit(endpoint);

        var connectionFactory = new ConnectionFactory();
        await using var connection = await connectionFactory.CreateAsync(endpoint);
        
        var address = Guid.NewGuid().ToString();

        var timestamp = DateTimeOffset.UtcNow;

        await using var staleMessageConsumer = await connection.CreateConsumerAsync(new ConsumerConfiguration
        {
            Address = address,
            RoutingType = RoutingType.Anycast,
            FilterExpression = $"AMQTimestamp < {timestamp.ToUnixTimeMilliseconds()}"
        });
        await using var freshMessageConsumer = await connection.CreateConsumerAsync(new ConsumerConfiguration
        {
            Address = address,
            RoutingType = RoutingType.Anycast,
            FilterExpression = $"AMQTimestamp > {timestamp.ToUnixTimeMilliseconds()}"
        });
        
        for (int i = 0; i < 3; i++)
        {
            await testKit.SendMessageAsync(address, new Message("staleMessage") { CreationTime = timestamp.AddHours(-1).DateTime });
            await testKit.SendMessageAsync(address, new Message("freshMessage") { CreationTime = timestamp.AddHours(1).DateTime });
        }

        var freshMessages = await ReceiveMessages(freshMessageConsumer, 3);
        var staleMessages = await ReceiveMessages(staleMessageConsumer, 3);

        Assert.All(freshMessages, x => Assert.Equal("freshMessage", x.GetBody<string>()));
        Assert.Equal(3, freshMessages.Count);
        Assert.All(staleMessages, x => Assert.Equal("staleMessage", x.GetBody<string>()));
        Assert.Equal(3, staleMessages.Count);
    }    
    
    [Fact]
    public async Task Should_filter_messages_based_on_application_property()
    {
        var endpoint = EndpointUtil.GetUniqueEndpoint();
        using var testKit = new TestKit(endpoint);

        var connectionFactory = new ConnectionFactory();
        await using var connection = await connectionFactory.CreateAsync(endpoint);
        
        var address = "test_address";
        await using var redMessageConsumer = await connection.CreateConsumerAsync(new ConsumerConfiguration
        {
            Address = address,
            RoutingType = RoutingType.Multicast,
            FilterExpression = "color = 'red'"
        });
        await using var blueMessageConsumer = await connection.CreateConsumerAsync(new ConsumerConfiguration
        {
            Address = address,
            RoutingType = RoutingType.Multicast,
            FilterExpression = "color = 'blue'"
        });

        for (int i = 0; i < 3; i++)
        {
            await testKit.SendMessageAsync(address, new Message("red") { ApplicationProperties = { ["color"] = "red" } });
            await testKit.SendMessageAsync(address, new Message("blue") { ApplicationProperties = { ["color"] = "blue" } });
        }

        var redMessages = await ReceiveMessages(redMessageConsumer, 3);
        var blueMessages = await ReceiveMessages(blueMessageConsumer, 3);

        Assert.All(redMessages, x => Assert.Equal("red", x.GetBody<string>()));
        Assert.All(blueMessages, x => Assert.Equal("blue", x.GetBody<string>()));
    }
    
    [Fact]
    public async Task Should_filter_messages_when_property_used_in_filter_is_missing()
    {
        var endpoint = EndpointUtil.GetUniqueEndpoint();
        using var testKit = new TestKit(endpoint);

        var connectionFactory = new ConnectionFactory();
        await using var connection = await connectionFactory.CreateAsync(endpoint);
        
        var address = "test_address";
        await using var messageConsumer = await connection.CreateConsumerAsync(new ConsumerConfiguration
        {
            Address = address,
            RoutingType = RoutingType.Multicast,
            FilterExpression = "color IS NULL"
        });

        await testKit.SendMessageAsync(address, new Message("msg"));

        var message = await messageConsumer.ReceiveAsync();
        
        Assert.Equal("msg", message.GetBody<string>());
    }
    
    static async Task<IReadOnlyList<Message>> ReceiveMessages(IConsumer consumer, int count)
    {
        var messages = new List<Message>();
        try
        {
            for (int i = 0; i < count; i++)
            {
                var message = await consumer.ReceiveAsync(new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token);
                await consumer.AcceptAsync(message);
                messages.Add(message);
            }
        }
        catch (Exception)
        {
            // ignored
        }
        
        return messages;
    }
}