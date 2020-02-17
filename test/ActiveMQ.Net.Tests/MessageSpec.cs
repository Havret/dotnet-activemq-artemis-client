using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace ActiveMQ.Net.Tests
{
    public class MessageSpec : ActiveMQNetSpec
    {
        public MessageSpec(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task Should_set_and_get_message_properties()
        {
            using var host = CreateOpenedContainerHost();
            var messageProcessor = host.CreateMessageProcessor("a1");
            await using var connection = await CreateConnection(host.Endpoint);
            var producer = await connection.CreateProducerAsync("a1");

            var message = new Message("text");
            message.Properties.MessageId = "messageId";
            message.Properties.UserId = new byte[] { 1, 2, 3, 4, 5 };
            message.Properties.Subject = "subject";
            message.Properties.CorrelationId = "correlationId";
            message.Properties.ContentType = "application/json";
            message.Properties.ContentEncoding = "gzip";
            message.Properties.AbsoluteExpiryTime = new DateTime(1991, 1, 25, 0, 0, 0, DateTimeKind.Utc);
            message.Properties.CreationTime = new DateTime(1991, 8, 22, 0, 0, 0, DateTimeKind.Utc);
            message.Properties.GroupId = "groupId";
            message.Properties.GroupSequence = 112u;
            message.Properties.ReplyToGroupId = "replyToGroupId";

            await producer.SendAsync(message);

            var receivedMsg = messageProcessor.Dequeue(Timeout);
            Assert.Equal("messageId", receivedMsg.Properties.MessageId);
            Assert.Equal(new byte[] { 1, 2, 3, 4, 5 }, receivedMsg.Properties.UserId);
            Assert.Equal("subject", receivedMsg.Properties.Subject);
            Assert.Equal("application/json", receivedMsg.Properties.ContentType);
            Assert.Equal("gzip", receivedMsg.Properties.ContentEncoding);
            Assert.Equal(new DateTime(1991, 1, 25, 0, 0, 0, DateTimeKind.Utc), receivedMsg.Properties.AbsoluteExpiryTime);
            Assert.Equal(new DateTime(1991, 8, 22, 0, 0, 0, DateTimeKind.Utc), receivedMsg.Properties.CreationTime);
            Assert.Equal("groupId", receivedMsg.Properties.GroupId);
            Assert.Equal(112u, receivedMsg.Properties.GroupSequence);
            Assert.Equal("replyToGroupId", receivedMsg.Properties.ReplyToGroupId);
        }

        [Fact]
        public async Task Should_reset_message_properties()
        {
            using var host = CreateOpenedContainerHost();
            var messageProcessor = host.CreateMessageProcessor("a1");
            await using var connection = await CreateConnection(host.Endpoint);
            var producer = await connection.CreateProducerAsync("a1");

            var message = new Message("text");
            message.Properties.MessageId = "messageId";
            message.Properties.UserId = new byte[] { 1, 2, 3, 4, 5 };
            message.Properties.Subject = "subject";
            message.Properties.CorrelationId = "correlationId";
            message.Properties.ContentType = "application/json";
            message.Properties.ContentEncoding = "gzip";
            message.Properties.AbsoluteExpiryTime = new DateTime(1991, 1, 25, 0, 0, 0, DateTimeKind.Utc);
            message.Properties.CreationTime = new DateTime(1991, 8, 22, 0, 0, 0, DateTimeKind.Utc);
            message.Properties.GroupId = "groupId";
            message.Properties.GroupSequence = 112u;
            message.Properties.ReplyToGroupId = "replyToGroupId";

            await producer.SendAsync(message);

            var receivedMsg = messageProcessor.Dequeue(Timeout);

            receivedMsg.Properties.MessageId = null;
            receivedMsg.Properties.UserId = null;
            receivedMsg.Properties.Subject = null;
            receivedMsg.Properties.ContentType = null;
            receivedMsg.Properties.ContentEncoding = null;
            receivedMsg.Properties.AbsoluteExpiryTime = null;
            receivedMsg.Properties.CreationTime = null;
            receivedMsg.Properties.GroupId = null;
            receivedMsg.Properties.GroupSequence = null;
            receivedMsg.Properties.ReplyToGroupId = null;

            await producer.SendAsync(receivedMsg);
            
            var resetMsg = messageProcessor.Dequeue(Timeout);
            
            Assert.Null(resetMsg.Properties.MessageId);
            Assert.Null(resetMsg.Properties.UserId);
            Assert.Null(resetMsg.Properties.Subject);
            Assert.Null(resetMsg.Properties.ContentType);
            Assert.Null(resetMsg.Properties.ContentEncoding);
            Assert.Null(resetMsg.Properties.AbsoluteExpiryTime);
            Assert.Null(resetMsg.Properties.CreationTime);
            Assert.Null(resetMsg.Properties.GroupId);
            Assert.Null(resetMsg.Properties.GroupSequence);
            Assert.Null(resetMsg.Properties.ReplyToGroupId);
        }
    }
}