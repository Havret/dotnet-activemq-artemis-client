﻿using System;
using System.Threading.Tasks;
using ActiveMQ.Net.Tests.Utils;
using Amqp.Framing;
using Xunit;

namespace ActiveMQ.Net.Tests
{
    public class ConsumerMessageAcknowledgementSpec
    {
        [Fact]
        public async Task Should_not_send_any_disposition_frames_until_message_is_accepted()
        {
            var address = AddressUtil.GetAddress();
            using var host = new TestContainerHost(address);
            host.Open();
            
            var messageSource = host.CreateMessageSource("a1");
            await using var connection = await CreateConnection(address);
            var consumer = await connection.CreateConsumerAsync("a1");
            
            messageSource.Enqueue(new Message("foo"));
            var message = await consumer.ConsumeAsync();

            Assert.NotNull(message);
            var dispositionContext = messageSource.GetNextDisposition(TimeSpan.FromMilliseconds(100));
            Assert.Null(dispositionContext);
        }
        
        [Fact]
        public async Task Should_send_accepted_and_settled_disposition_frame_when_message_accepted()
        {
            var address = AddressUtil.GetAddress();
            using var host = new TestContainerHost(address);
            host.Open();
            
            var messageSource = host.CreateMessageSource("a1");
            await using var connection = await CreateConnection(address);
            var consumer = await connection.CreateConsumerAsync("a1");
            
            messageSource.Enqueue(new Message("foo"));
            var message = await consumer.ConsumeAsync();
            consumer.Accept(message);

            var dispositionContext = messageSource.GetNextDisposition(TimeSpan.FromSeconds(1));
            Assert.IsType<Accepted>(dispositionContext.DeliveryState);
            Assert.True(dispositionContext.Settled);
        }

        [Fact]
        public async Task Should_send_rejected_disposition_frame_when_message_rejected()
        {
            var address = AddressUtil.GetAddress();
            using var host = new TestContainerHost(address);
            host.Open();
            
            var messageSource = host.CreateMessageSource("a1");
            await using var connection = await CreateConnection(address);
            var consumer = await connection.CreateConsumerAsync("a1");
            
            messageSource.Enqueue(new Message("foo"));
            var message = await consumer.ConsumeAsync();
            consumer.Reject(message);

            var dispositionContext = messageSource.GetNextDisposition(TimeSpan.FromSeconds(1));
            Assert.IsType<Rejected>(dispositionContext.DeliveryState);
            Assert.True(dispositionContext.Settled);
        }

        private static Task<IConnection> CreateConnection(string address)
        {
            var connectionFactory = new ConnectionFactory();
            return connectionFactory.CreateAsync(address);
        }
    }
}