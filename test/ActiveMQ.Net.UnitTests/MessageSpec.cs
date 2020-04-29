using System;
using System.Threading.Tasks;
using Amqp.Types;
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
            var producer = await connection.CreateProducerAsync("a1", AddressRoutingType.Anycast);

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
            var producer = await connection.CreateProducerAsync("a1", AddressRoutingType.Anycast);

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

        [Fact]
        public async Task Should_set_and_get_message_application_properties()
        {
            using var host = CreateOpenedContainerHost();
            var messageProcessor = host.CreateMessageProcessor("a1");
            await using var connection = await CreateConnection(host.Endpoint);
            var producer = await connection.CreateProducerAsync("a1", AddressRoutingType.Anycast);

            var message = new Message("text");
            message.ApplicationProperties["charKey"] = 'c';
            message.ApplicationProperties["stringKey"] = "stringValue";
            message.ApplicationProperties["boolKey"] = true;
            message.ApplicationProperties["byteKey"] = byte.MaxValue;
            message.ApplicationProperties["sbyteKey"] = sbyte.MaxValue;
            message.ApplicationProperties["shortKey"] = short.MaxValue;
            message.ApplicationProperties["ushortKey"] = ushort.MaxValue;
            message.ApplicationProperties["intKey"] = int.MaxValue;
            message.ApplicationProperties["uintKey"] = uint.MaxValue;
            message.ApplicationProperties["longKey"] = long.MaxValue;
            message.ApplicationProperties["ulongKey"] = ulong.MaxValue;
            message.ApplicationProperties["floatKey"] = float.MaxValue;
            message.ApplicationProperties["doubleKey"] = double.MaxValue;
            message.ApplicationProperties["DateTimeKey"] = DateTime.Today.ToUniversalTime();
            message.ApplicationProperties["GuidKey"] = Guid.Parse("D50A5C8B-3EDE-4FA0-93E5-5B6AB38EEA3E");
            message.ApplicationProperties["ArrayIntKey"] = new[] { 1, 2, 3 };
            message.ApplicationProperties["MapKey"] = new Map
            {
                { "testKey", "testValue" },
                {
                    "testMapKey", new Map
                    {
                        { "innerKey", "innerValue" }
                    }
                },
            };

            await producer.SendAsync(message);

            var resetMsg = messageProcessor.Dequeue(Timeout);

            Assert.Equal('c', resetMsg.ApplicationProperties["charKey"]);
            Assert.Equal("stringValue", resetMsg.ApplicationProperties["stringKey"]);
            Assert.Equal(true, resetMsg.ApplicationProperties["boolKey"]);
            Assert.Equal(byte.MaxValue, resetMsg.ApplicationProperties["byteKey"]);
            Assert.Equal(sbyte.MaxValue, resetMsg.ApplicationProperties["sbyteKey"]);
            Assert.Equal(short.MaxValue, resetMsg.ApplicationProperties["shortKey"]);
            Assert.Equal(ushort.MaxValue, resetMsg.ApplicationProperties["ushortKey"]);
            Assert.Equal(int.MaxValue, resetMsg.ApplicationProperties["intKey"]);
            Assert.Equal(uint.MaxValue, resetMsg.ApplicationProperties["uintKey"]);
            Assert.Equal(long.MaxValue, resetMsg.ApplicationProperties["longKey"]);
            Assert.Equal(ulong.MaxValue, resetMsg.ApplicationProperties["ulongKey"]);
            Assert.Equal(float.MaxValue, resetMsg.ApplicationProperties["floatKey"]);
            Assert.Equal(double.MaxValue, resetMsg.ApplicationProperties["doubleKey"]);
            Assert.Equal(double.MaxValue, resetMsg.ApplicationProperties["doubleKey"]);
            Assert.Equal(DateTime.Today.ToUniversalTime(), resetMsg.ApplicationProperties["DateTimeKey"]);
            Assert.Equal(Guid.Parse("D50A5C8B-3EDE-4FA0-93E5-5B6AB38EEA3E"), resetMsg.ApplicationProperties["GuidKey"]);
            Assert.Equal(new[] { 1, 2, 3 }, resetMsg.ApplicationProperties["ArrayIntKey"]);
            Assert.Equal(new Map
            {
                { "testKey", "testValue" },
                {
                    "testMapKey", new Map
                    {
                        { "innerKey", "innerValue" }
                    }
                },
            }, resetMsg.ApplicationProperties["MapKey"]);
        }

        [Fact]
        public Task Should_send_message_with_char_payload() => ShouldSendMessageWithPayload(char.MaxValue);

        [Fact]
        public Task Should_send_message_with_string_payload() => ShouldSendMessageWithPayload("foo");

        [Fact]
        public Task Should_send_message_with_byte_payload() => ShouldSendMessageWithPayload(byte.MaxValue);

        [Fact]
        public Task Should_send_message_with_sbyte_payload() => ShouldSendMessageWithPayload(sbyte.MaxValue);

        [Fact]
        public Task Should_send_message_with_short_payload() => ShouldSendMessageWithPayload(short.MaxValue);

        [Fact]
        public Task Should_send_message_with_ushort_payload() => ShouldSendMessageWithPayload(ushort.MaxValue);

        [Fact]
        public Task Should_send_message_with_int_payload() => ShouldSendMessageWithPayload(int.MaxValue);

        [Fact]
        public Task Should_send_message_with_uint_payload() => ShouldSendMessageWithPayload(uint.MaxValue);

        [Fact]
        public Task Should_send_message_with_long_payload() => ShouldSendMessageWithPayload(long.MaxValue);

        [Fact]
        public Task Should_send_message_with_ulong_payload() => ShouldSendMessageWithPayload(ulong.MaxValue);

        [Fact]
        public Task Should_send_message_with_float_payload() => ShouldSendMessageWithPayload(float.MaxValue);

        [Fact]
        public Task Should_send_message_with_double_payload() => ShouldSendMessageWithPayload(double.MaxValue);

        [Fact]
        public Task Should_send_message_with_Guid_payload() => ShouldSendMessageWithPayload(Guid.NewGuid());

        [Fact]
        public Task Should_send_message_with_DateTime_payload()
        {
            // drop tics precision, as AMQP timestamp is represented as milliseconds from Unix epoch
            const long ticksPerMillisecond = 10000;
            var dateTime = new DateTime(DateTime.UtcNow.Ticks / ticksPerMillisecond * ticksPerMillisecond, DateTimeKind.Utc);
            
            return ShouldSendMessageWithPayload(dateTime);
        }

        [Fact]
        public Task Should_send_message_with_bytes_payload() => ShouldSendMessageWithPayload(new byte[] { 1, 2, 3, 4 });

        [Fact]
        public Task Should_send_message_with_List_payload() => ShouldSendMessageWithPayload(new List
        {
            char.MaxValue, "foo", byte.MaxValue, sbyte.MaxValue, short.MaxValue, ushort.MaxValue, int.MaxValue, uint.MaxValue, long.MaxValue, ulong.MaxValue, float.MaxValue, double.MaxValue, new byte[] { 1, 2, 3, 4 },
            new List { 1, 2, 3, 4 }
        });

        private async Task ShouldSendMessageWithPayload<T>(T payload)
        {
            using var host = CreateOpenedContainerHost();
            var messageProcessor = host.CreateMessageProcessor("a1");
            await using var connection = await CreateConnection(host.Endpoint);
            await using var producer = await connection.CreateProducerAsync("a1", AddressRoutingType.Anycast);

            var message = new Message(payload);
            await producer.SendAsync(message);

            var received = messageProcessor.Dequeue(Timeout);

            Assert.Equal(payload, received.GetBody<T>());
        }
    }
}