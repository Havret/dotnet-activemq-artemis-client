using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.TestUtils;
using ActiveMQ.Artemis.Client.Transactions;
using Xunit;
using System.Linq;
using System.Threading;

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
    
    [Fact]
    public async Task Should_send_message_to_multiple_shared_consumers()
    {
        var endpoint = EndpointUtil.GetUniqueEndpoint();
        using var testKit = new TestKit(endpoint);

        var connectionFactory = new ConnectionFactory();
        await using var connection = await connectionFactory.CreateAsync(endpoint);
        
        var testAddress = "test_address";
        var testQueue = "testQueue";
        await using var consumer1 = await connection.CreateConsumerAsync(new ConsumerConfiguration
        {
            Address = testAddress,
            Queue = testQueue,
            Shared = true
        });
        await using var consumer2 = await connection.CreateConsumerAsync(new ConsumerConfiguration
        {
            Address = testAddress,
            Queue = testQueue,
            Shared = true
        });

        await testKit.SendMessageAsync(testAddress, new Message("foo1"));
        await testKit.SendMessageAsync(testAddress, new Message("foo2"));
        await testKit.SendMessageAsync(testAddress, new Message("foo3"));
        await testKit.SendMessageAsync(testAddress, new Message("foo4"));

        // Round-robin - messages are evenly distributed among consumers
        Assert.Equal("foo1", (await consumer1.ReceiveAsync()).GetBody<string>());
        Assert.Equal("foo3", (await consumer1.ReceiveAsync()).GetBody<string>());
        
        Assert.Equal("foo2", (await consumer2.ReceiveAsync()).GetBody<string>());
        Assert.Equal("foo4", (await consumer2.ReceiveAsync()).GetBody<string>());
    }
    
    [Fact]
    public async Task Should_deliver_messages_with_the_same_GroupId_to_the_same_consumer()
    {
        var endpoint = EndpointUtil.GetUniqueEndpoint();
        using var testKit = new TestKit(endpoint);

        var connectionFactory = new ConnectionFactory();
        await using var connection = await connectionFactory.CreateAsync(endpoint);
        
        var testAddress = "test_address";
        var testQueue = "testQueue";
        await using var consumer1 = await connection.CreateConsumerAsync(new ConsumerConfiguration
        {
            Address = testAddress,
            Queue = testQueue,
            Shared = true
        });
        await using var consumer2 = await connection.CreateConsumerAsync(new ConsumerConfiguration
        {
            Address = testAddress,
            Queue = testQueue,
            Shared = true
        });


        await SendMessagesToGroup(testKit, testAddress, "group1", 5);
        await SendMessagesToGroup(testKit, testAddress, "group2", 5);

        await AssertReceivedAllMessagesWithTheSameGroupId(consumer1, 5);
        await AssertReceivedAllMessagesWithTheSameGroupId(consumer2, 5);
        
        static async Task SendMessagesToGroup(TestKit testKit, string testAddress, string groupId, int count)
        {
            for (int i = 1; i <= count; i++)
            {
                await testKit.SendMessageAsync(testAddress, new Message(groupId)
                {
                    GroupId = groupId,
                    GroupSequence = (uint) i
                });
            }
        }
        
        static async Task AssertReceivedAllMessagesWithTheSameGroupId(IConsumer consumer, int count)
        {
            var messages = new List<Message>();
            try
            {
                for (int i = 1; i <= count; i++)
                {
                    var message = await consumer.ReceiveAsync(new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token);
                    Assert.Equal((uint) i, message.GroupSequence);
                    messages.Add(message);
                }
            }
            catch (Exception)
            {
                // ignored
            }

            Assert.Single(messages.GroupBy(x => x.GroupId));
            Assert.Equal(count, messages.Count);
        }
    }
}