using System;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace ActiveMQ.Artemis.Client.Extensions.AspNetCore.IntegrationTests;

public class RequestReplyClientSpec
{
    private readonly ITestOutputHelper _testOutputHelper;

    public RequestReplyClientSpec(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public async Task Should_register_request_reply_client()
    {
        await using var testFixture = await TestFixture.CreateAsync(_testOutputHelper, activeMqBuilder =>
        {
            activeMqBuilder.AddRequestReplyClient<TestRequestReplyClient>();
        });

        var requestReplyClient = testFixture.Services.GetRequiredService<TestRequestReplyClient>();

        Assert.NotNull(requestReplyClient);
    }

    [Fact]
    public async Task Should_send_and_receive_message_using_registered_request_reply_client()
    {
        await using var testFixture = await TestFixture.CreateAsync(_testOutputHelper, activeMqBuilder =>
        {
            activeMqBuilder.AddRequestReplyClient<TestRequestReplyClient>();
        });

        var address = Guid.NewGuid().ToString();
        var replyOnTask = ReplyOn(address, testFixture.Connection, testFixture.CancellationToken);

        var requestReplyClient = testFixture.Services.GetRequiredService<TestRequestReplyClient>();

        var responseTask = requestReplyClient.SendMessage(address, testFixture.CancellationToken);

        await Task.WhenAll(replyOnTask, responseTask);

        var response = responseTask.Result;

        Assert.Equal("reply", response.GetBody<string>());
    }
    
    private static Task ReplyOn(string address, IConnection connection, CancellationToken cancellationToken)
    {
        return Task.Run(async () =>
        {
            await using var consumer = await connection.CreateConsumerAsync(address, RoutingType.Anycast, cancellationToken);
            await using var producer = await connection.CreateAnonymousProducerAsync(cancellationToken);
            var request = await consumer.ReceiveAsync(cancellationToken);
            await producer.SendAsync(request.ReplyTo, new Message("reply")
            {
                CorrelationId = request.CorrelationId
            }, cancellationToken);
            await consumer.AcceptAsync(request);
        }, cancellationToken);
    }
    
    [Fact]
    public async Task Throws_when_request_reply_client_with_the_same_type_registered_twice()
    {
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => TestFixture.CreateAsync(_testOutputHelper, activeMqBuilder =>
        {
            activeMqBuilder.AddRequestReplyClient<TestRequestReplyClient>();
            activeMqBuilder.AddRequestReplyClient<TestRequestReplyClient>();
        }));

        Assert.Contains($"There has already been registered Request Reply Client with the type '{typeof(TestRequestReplyClient).FullName}'.", exception.Message);
    }
    
    private class TestRequestReplyClient
    {
        private readonly IRequestReplyClient _client;

        public TestRequestReplyClient(IRequestReplyClient client)
        {
            _client = client;
        }

        public Task<Message> SendMessage(string address, CancellationToken cancellationToken)
        {
            return _client.SendAsync(address, RoutingType.Anycast, new Message("request"), cancellationToken);
        }
    }

    [Fact]
    public async Task Should_register_multiple_request_reply_clients()
    {
        await using var testFixture = await TestFixture.CreateAsync(_testOutputHelper, activeMqBuilder =>
        {
            activeMqBuilder.AddRequestReplyClient<TestRequestReplyClient1>();
            activeMqBuilder.AddRequestReplyClient<TestRequestReplyClient2>();
        });

        var testProducer1 = testFixture.Services.GetRequiredService<TestRequestReplyClient1>();
        var testProducer2 = testFixture.Services.GetRequiredService<TestRequestReplyClient2>();

        Assert.NotEqual(testProducer1.Client, testProducer2.Client);
    }
    private class TestRequestReplyClient1
    {
        public IRequestReplyClient Client { get; }
        public TestRequestReplyClient1(IRequestReplyClient client) => Client = client;
    }

    private class TestRequestReplyClient2
    {
        public IRequestReplyClient Client { get; }
        public TestRequestReplyClient2(IRequestReplyClient client) => Client = client;
    }
}