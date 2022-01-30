using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace ActiveMQ.Artemis.Client.IntegrationTests;

public class RequestReplyClientSpec : ActiveMQNetIntegrationSpec
{
    public RequestReplyClientSpec(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure")]
    public async Task Should_send_request_and_receive_response_message()
    {
        // Arrange
        await using var connection1 = await CreateConnection();
        var address = Guid.NewGuid().ToString();
        await using var consumer = await connection1.CreateConsumerAsync(address, RoutingType.Anycast);
        await using var producer = await connection1.CreateAnonymousProducerAsync();

        var _ = Task.Run(async () =>
        {
            var request = await consumer.ReceiveAsync();
            await producer.SendAsync(request.ReplyTo, new Message("bar")
            {
                CorrelationId = request.CorrelationId
            });
            await consumer.AcceptAsync(request);
        });

        await using var connection2 = await CreateConnection();
        await using var rpcClientAsync = await connection2.CreateRequestReplyClientAsync();
        
        // Act
        var response = await rpcClientAsync.SendAsync(address, RoutingType.Anycast, new Message("foo"), default);

        // Assert
        Assert.Equal("bar", response.GetBody<string>());
    }
}