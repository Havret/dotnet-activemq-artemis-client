using System;
using System.Linq;
using System.Threading.Tasks;
using Amqp.Types;
using Xunit;
using Xunit.Abstractions;
using ActiveMQ.Artemis.Client.InternalUtilities;

namespace ActiveMQ.Artemis.Client.UnitTests
{
    public class MessageSpec : ActiveMQNetSpec
    {
        public MessageSpec(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task Should_set_and_get_message_application_properties()
        {
            using var host = CreateOpenedContainerHost();
            var messageProcessor = host.CreateMessageProcessor("a1");
            await using var connection = await CreateConnection(host.Endpoint);
            var producer = await connection.CreateProducerAsync("a1", RoutingType.Anycast);

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
        public async Task Should_return_false_when_application_property_not_specified_for_given_key()
        {
            using var host = CreateOpenedContainerHost();
            var messageProcessor = host.CreateMessageProcessor("a1");
            await using var connection = await CreateConnection(host.Endpoint);
            await using var producer = await connection.CreateProducerAsync("a1", RoutingType.Anycast);
            
            var message = new Message("foo");
            await producer.SendAsync(message);

            var received = messageProcessor.Dequeue(ShortTimeout);

            Assert.False(received.ApplicationProperties.ContainsKey("key"));
        }
        
        [Fact]
        public async Task Should_return_true_when_application_property_specified_for_given_key()
        {
            using var host = CreateOpenedContainerHost();
            var messageProcessor = host.CreateMessageProcessor("a1");
            await using var connection = await CreateConnection(host.Endpoint);
            await using var producer = await connection.CreateProducerAsync("a1", RoutingType.Anycast);

            var message = new Message("foo")
            {
                ApplicationProperties = { ["key"] = "value" }
            };
            await producer.SendAsync(message);

            var received = messageProcessor.Dequeue(ShortTimeout);

            Assert.True(received.ApplicationProperties.ContainsKey("key"));
        }

        [Fact]
        public async Task Should_not_get_value_when_application_property_not_specified_for_given_key()
        {
            using var host = CreateOpenedContainerHost();
            var messageProcessor = host.CreateMessageProcessor("a1");
            await using var connection = await CreateConnection(host.Endpoint);
            await using var producer = await connection.CreateProducerAsync("a1", RoutingType.Anycast);
            
            var message = new Message("foo");
            await producer.SendAsync(message);

            var received = messageProcessor.Dequeue(ShortTimeout);
            Assert.False(received.ApplicationProperties.TryGetValue("key", out string value));
            Assert.Null(value);
        }
        
        [Fact]
        public async Task Should_not_get_value_when_application_property_type_does_not_match_for_given_key()
        {
            using var host = CreateOpenedContainerHost();
            var messageProcessor = host.CreateMessageProcessor("a1");
            await using var connection = await CreateConnection(host.Endpoint);
            await using var producer = await connection.CreateProducerAsync("a1", RoutingType.Anycast);
            
            var message = new Message("foo")
            {
                ApplicationProperties = { ["key"] = "value" }
            };
            await producer.SendAsync(message);

            var received = messageProcessor.Dequeue(ShortTimeout);
            Assert.False(received.ApplicationProperties.TryGetValue("key", out int value));
            Assert.Equal(default, value);
        }
        
        [Fact]
        public async Task Should_get_value_when_application_property_specified_for_given_key()
        {
            using var host = CreateOpenedContainerHost();
            var messageProcessor = host.CreateMessageProcessor("a1");
            await using var connection = await CreateConnection(host.Endpoint);
            await using var producer = await connection.CreateProducerAsync("a1", RoutingType.Anycast);
            
            var message = new Message("foo")
            {
                ApplicationProperties = { ["key"] = "value" }
            };
            await producer.SendAsync(message);

            var received = messageProcessor.Dequeue(ShortTimeout);
            Assert.True(received.ApplicationProperties.TryGetValue("key", out string value));
            Assert.Equal("value",value);
        }

        [Fact]
        public async Task Should_get_all_the_application_property_keys()
        {
            using var host = CreateOpenedContainerHost();
            var messageProcessor = host.CreateMessageProcessor("a1");
            await using var connection = await CreateConnection(host.Endpoint);
            await using var producer = await connection.CreateProducerAsync("a1", RoutingType.Anycast);

            var message = new Message("foo")
            {
                ApplicationProperties =
                {
                    ["key1"] = "value1",
                    ["key2"] = 2,
                    ["key3"] = 2.5d,
                }
            };
            await producer.SendAsync(message);

            var received = messageProcessor.Dequeue(ShortTimeout);

            Assert.Equal(3, received.ApplicationProperties.Keys.Count());
            Assert.Contains("key1", received.ApplicationProperties.Keys);
            Assert.Contains("key2", received.ApplicationProperties.Keys);
            Assert.Contains("key3", received.ApplicationProperties.Keys);
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
        public Task Should_send_message_with_DateTime_payload() => ShouldSendMessageWithPayload(DateTime.UtcNow.DropTicsPrecision());

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
            await using var producer = await connection.CreateProducerAsync("a1", RoutingType.Anycast);

            var message = new Message(payload);
            await producer.SendAsync(message);

            var received = messageProcessor.Dequeue(Timeout);

            Assert.Equal(payload, received.GetBody<T>());
        }
        
        [Fact]
        public async Task Should_send_message_without_GroupId_set()
        {
            using var host = CreateOpenedContainerHost();
            var messageProcessor = host.CreateMessageProcessor("a1");
            await using var connection = await CreateConnection(host.Endpoint);
            await using var producer = await connection.CreateProducerAsync("a1", RoutingType.Anycast);

            await producer.SendAsync(new Message("foo"));

            var received = messageProcessor.Dequeue(Timeout);

            Assert.Null(received.GroupId);
        }

        [Fact]
        public async Task Should_send_message_with_GroupId_set()
        {
            using var host = CreateOpenedContainerHost();
            var messageProcessor = host.CreateMessageProcessor("a1");
            await using var connection = await CreateConnection(host.Endpoint);
            await using var producer = await connection.CreateProducerAsync("a1", RoutingType.Anycast);

            var message = new Message("foo")
            {
                GroupId = "bar"
            };
            await producer.SendAsync(message);

            var received = messageProcessor.Dequeue(Timeout);

            Assert.Equal("bar", received.GroupId);
        }
        
        [Fact]
        public async Task Should_send_message_without_GroupSequence_set()
        {
            using var host = CreateOpenedContainerHost();
            var messageProcessor = host.CreateMessageProcessor("a1");
            await using var connection = await CreateConnection(host.Endpoint);
            await using var producer = await connection.CreateProducerAsync("a1", RoutingType.Anycast);

            await producer.SendAsync(new Message("foo"));

            var received = messageProcessor.Dequeue(Timeout);

            Assert.Null(received.GroupSequence);
        }

        [Fact]
        public async Task Should_send_message_with_GroupSequence_set()
        {
            using var host = CreateOpenedContainerHost();
            var messageProcessor = host.CreateMessageProcessor("a1");
            await using var connection = await CreateConnection(host.Endpoint);
            await using var producer = await connection.CreateProducerAsync("a1", RoutingType.Anycast);

            var message = new Message("foo")
            {
                GroupSequence = 1u
            };
            await producer.SendAsync(message);

            var received = messageProcessor.Dequeue(Timeout);

            Assert.Equal(1u, received.GroupSequence);
        }

        [Fact]
        public async Task Should_send_message_without_TimeToLive_set()
        {
            using var host = CreateOpenedContainerHost();
            var messageProcessor = host.CreateMessageProcessor("a1");
            await using var connection = await CreateConnection(host.Endpoint);
            await using var producer = await connection.CreateProducerAsync("a1", RoutingType.Anycast);

            await producer.SendAsync(new Message("foo"));

            var received = messageProcessor.Dequeue(Timeout);

            Assert.Null(received.TimeToLive);
        }

        [Fact]
        public async Task Should_send_message_with_TimeToLive_set()
        {
            using var host = CreateOpenedContainerHost();
            var messageProcessor = host.CreateMessageProcessor("a1");
            await using var connection = await CreateConnection(host.Endpoint);
            await using var producer = await connection.CreateProducerAsync("a1", RoutingType.Anycast);

            var message = new Message("foo")
            {
                TimeToLive = TimeSpan.FromMilliseconds(10)
            };
            await producer.SendAsync(message);

            var received = messageProcessor.Dequeue(Timeout);

            Assert.Equal(TimeSpan.FromMilliseconds(10), received.TimeToLive);
        }

        [Fact]
        public async Task Should_send_message_without_ScheduledDeliveryTime_set()
        {
            using var host = CreateOpenedContainerHost();
            var messageProcessor = host.CreateMessageProcessor("a1");
            await using var connection = await CreateConnection(host.Endpoint);
            await using var producer = await connection.CreateProducerAsync("a1", RoutingType.Anycast);

            var message = new Message("foo")
            {
                ScheduledDeliveryTime = null
            };
            await producer.SendAsync(message);

            var received = messageProcessor.Dequeue(Timeout);

            Assert.Null(received.ScheduledDeliveryTime);
        }
        
        [Fact]
        public async Task Should_send_message_with_ScheduledDeliveryTime_set()
        {
            using var host = CreateOpenedContainerHost();
            var messageProcessor = host.CreateMessageProcessor("a1");
            await using var connection = await CreateConnection(host.Endpoint);
            await using var producer = await connection.CreateProducerAsync("a1", RoutingType.Anycast);

            var scheduledDeliveryTime = DateTime.UtcNow.AddHours(1).DropTicsPrecision();
            var message = new Message("foo")
            {
                ScheduledDeliveryTime = scheduledDeliveryTime
            };
            await producer.SendAsync(message);

            var received = messageProcessor.Dequeue(Timeout);

            Assert.Equal(scheduledDeliveryTime, received.ScheduledDeliveryTime);
        }
        
        [Fact]
        public async Task Should_send_message_without_ScheduledDeliveryDelay_set()
        {
            using var host = CreateOpenedContainerHost();
            var messageProcessor = host.CreateMessageProcessor("a1");
            await using var connection = await CreateConnection(host.Endpoint);
            await using var producer = await connection.CreateProducerAsync("a1", RoutingType.Anycast);

            var message = new Message("foo")
            {
                ScheduledDeliveryDelay = null
            };
            await producer.SendAsync(message);

            var received = messageProcessor.Dequeue(Timeout);

            Assert.Null(received.ScheduledDeliveryDelay);
        }
        
        [Fact]
        public async Task Should_send_message_with_ScheduledDeliveryDelay_set()
        {
            using var host = CreateOpenedContainerHost();
            var messageProcessor = host.CreateMessageProcessor("a1");
            await using var connection = await CreateConnection(host.Endpoint);
            await using var producer = await connection.CreateProducerAsync("a1", RoutingType.Anycast);
        
            var scheduledDeliveryDelay = TimeSpan.FromHours(1);
            var message = new Message("foo")
            {
                ScheduledDeliveryDelay = scheduledDeliveryDelay
            };
            await producer.SendAsync(message);
        
            var received = messageProcessor.Dequeue(Timeout);
        
            Assert.Equal(scheduledDeliveryDelay, received.ScheduledDeliveryDelay);
        }

        [Fact]
        public async Task Should_send_message_with_CorrelationId()
        {
            using var host = CreateOpenedContainerHost();
            var messageProcessor = host.CreateMessageProcessor("a1");
            await using var connection = await CreateConnection(host.Endpoint);
            await using var producer = await connection.CreateProducerAsync("a1", RoutingType.Anycast);
            
            var message = new Message("foo")
            {
                CorrelationId = "correlation"
            };
            await producer.SendAsync(message);

            var received = messageProcessor.Dequeue(ShortTimeout);
            
            Assert.Equal("correlation", received.CorrelationId);
        }

        [Fact]
        public Task Should_sent_message_with_string_CorrelationId() => ShouldSendMessageWithCorrelationId("correlationId");
        
        [Fact]
        public Task Should_sent_message_with_Guid_CorrelationId() => ShouldSendMessageWithCorrelationId(Guid.NewGuid());
        
        [Fact]
        public Task Should_sent_message_with_ulong_CorrelationId() => ShouldSendMessageWithCorrelationId(ulong.MaxValue);
        
        [Fact]
        public Task Should_sent_message_with_bytes_CorrelationId() => ShouldSendMessageWithCorrelationId(Guid.NewGuid().ToByteArray());

        private async Task ShouldSendMessageWithCorrelationId<T>(T correlationId)
        {
            using var host = CreateOpenedContainerHost();
            var messageProcessor = host.CreateMessageProcessor("a1");
            await using var connection = await CreateConnection(host.Endpoint);
            await using var producer = await connection.CreateProducerAsync("a1", RoutingType.Anycast);
            
            var message = new Message("foo");
            message.SetCorrelationId(correlationId);
            await producer.SendAsync(message);

            var received = messageProcessor.Dequeue(ShortTimeout);
            
            Assert.Equal(correlationId, received.GetCorrelationId<T>());
        }
    }
}