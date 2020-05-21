using System;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Transactions;
using Xunit;
using Xunit.Abstractions;

namespace ActiveMQ.Artemis.Client.IntegrationTests
{
    public class TransactionsSpec : ActiveMQNetIntegrationSpec
    {
        public TransactionsSpec(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task Should_deliver_all_sent_messages_when_transaction_committed()
        {
            await using var connection = await CreateConnection();
            var address = Guid.NewGuid().ToString();
            await using var producer = await connection.CreateProducerAsync(address, AddressRoutingType.Anycast);
            await using var consumer = await connection.CreateConsumerAsync(address, QueueRoutingType.Anycast);

            await using var transaction = new Transaction();
            await producer.SendAsync(new Message("foo1"), transaction);
            await producer.SendAsync(new Message("foo2"), transaction);

            await Assert.ThrowsAsync<OperationCanceledException>(async () => await consumer.ReceiveAsync(new CancellationTokenSource(TimeSpan.FromMilliseconds(500)).Token));

            await transaction.CommitAsync();

            var msg1 = await consumer.ReceiveAsync(CancellationToken);
            var msg2 = await consumer.ReceiveAsync(CancellationToken);
            Assert.Equal("foo1", msg1.GetBody<string>());
            Assert.Equal("foo2", msg2.GetBody<string>());

            await consumer.AcceptAsync(msg1);
            await consumer.AcceptAsync(msg2);
        }

        [Fact]
        public async Task Should_not_deliver_any_messages_when_transaction_rolled_back()
        {
            await using var connection = await CreateConnection();
            var address = Guid.NewGuid().ToString();
            await using var producer = await connection.CreateProducerAsync(address, AddressRoutingType.Anycast);
            await using var consumer = await connection.CreateConsumerAsync(address, QueueRoutingType.Anycast);

            await using var transaction = new Transaction();
            await producer.SendAsync(new Message("foo1"), transaction);
            await producer.SendAsync(new Message("foo2"), transaction);

            await transaction.RollbackAsync();

            await Assert.ThrowsAsync<OperationCanceledException>(async () => await consumer.ReceiveAsync(new CancellationTokenSource(TimeSpan.FromMilliseconds(500)).Token));
        }

        [Fact]
        public async Task Should_handle_two_transactions_independently_using_one_producer()
        {
            await using var connection = await CreateConnection();
            var address = Guid.NewGuid().ToString();
            await using var producer = await connection.CreateProducerAsync(address, AddressRoutingType.Anycast);
            await using var consumer = await connection.CreateConsumerAsync(address, QueueRoutingType.Anycast);

            await using var transaction1 = new Transaction();
            await using var transaction2 = new Transaction();
            await producer.SendAsync(new Message(1), transaction1);
            await producer.SendAsync(new Message(2), transaction1);
            await producer.SendAsync(new Message(3), transaction2);
            await producer.SendAsync(new Message(4), transaction2);

            await transaction1.CommitAsync();
            await transaction2.CommitAsync();

            for (int i = 1; i <= 4; i++)
            {
                var msg = await consumer.ReceiveAsync(CancellationToken);
                Assert.Equal(i, msg.GetBody<int>());
                await consumer.AcceptAsync(msg);
            }
        }

        [Fact]
        public async Task Should_handle_two_transactions_independently_using_one_producer_when_first_committed_and_second_rolled_back()
        {
            await using var connection = await CreateConnection();
            var address = Guid.NewGuid().ToString();
            await using var producer = await connection.CreateProducerAsync(address, AddressRoutingType.Anycast);
            await using var consumer = await connection.CreateConsumerAsync(address, QueueRoutingType.Anycast);

            await using var transaction1 = new Transaction();
            await using var transaction2 = new Transaction();
            await producer.SendAsync(new Message(1), transaction1);
            await producer.SendAsync(new Message(2), transaction1);
            await producer.SendAsync(new Message(3), transaction2);
            await producer.SendAsync(new Message(4), transaction2);

            await transaction1.CommitAsync();
            await transaction2.RollbackAsync();

            for (int i = 1; i <= 2; i++)
            {
                var msg = await consumer.ReceiveAsync(CancellationToken);
                Assert.Equal(i, msg.GetBody<int>());
                await consumer.AcceptAsync(msg);
            }

            await Assert.ThrowsAsync<OperationCanceledException>(async () => await consumer.ReceiveAsync(new CancellationTokenSource(TimeSpan.FromMilliseconds(500)).Token));
        }

        [Fact]
        public async Task Should_handle_two_transactions_using_two_producers()
        {
            await using var connection = await CreateConnection();
            var address = Guid.NewGuid().ToString();
            await using var producer1 = await connection.CreateProducerAsync(address, AddressRoutingType.Anycast);
            await using var producer2 = await connection.CreateProducerAsync(address, AddressRoutingType.Anycast);
            await using var consumer = await connection.CreateConsumerAsync(address, QueueRoutingType.Anycast);

            await using var transaction1 = new Transaction();
            await using var transaction2 = new Transaction();
            await producer1.SendAsync(new Message(1), transaction1);
            await producer1.SendAsync(new Message(2), transaction1);
            await producer2.SendAsync(new Message(3), transaction2);
            await producer2.SendAsync(new Message(4), transaction2);

            await transaction1.CommitAsync();
            await transaction2.CommitAsync();

            for (int i = 1; i <= 4; i++)
            {
                var msg = await consumer.ReceiveAsync(CancellationToken);
                Assert.Equal(i, msg.GetBody<int>());
                await consumer.AcceptAsync(msg);
            }
        }

        [Fact]
        public async Task Should_handle_two_transactions_using_two_producers_when_first_transaction_committed_and_second_rolled_back()
        {
            await using var connection = await CreateConnection();
            var address = Guid.NewGuid().ToString();
            await using var producer1 = await connection.CreateProducerAsync(address, AddressRoutingType.Anycast);
            await using var producer2 = await connection.CreateProducerAsync(address, AddressRoutingType.Anycast);
            await using var consumer = await connection.CreateConsumerAsync(address, QueueRoutingType.Anycast);

            var transaction1 = new Transaction();
            var transaction2 = new Transaction();
            await producer1.SendAsync(new Message(1), transaction1);
            await producer1.SendAsync(new Message(2), transaction1);
            await producer2.SendAsync(new Message(3), transaction2);
            await producer2.SendAsync(new Message(4), transaction2);

            await transaction1.CommitAsync();
            await transaction2.RollbackAsync();

            for (int i = 1; i <= 2; i++)
            {
                var msg = await consumer.ReceiveAsync(CancellationToken);
                Assert.Equal(i, msg.GetBody<int>());
                await consumer.AcceptAsync(msg);
            }

            await Assert.ThrowsAsync<OperationCanceledException>(async () => await consumer.ReceiveAsync(new CancellationTokenSource(TimeSpan.FromMilliseconds(500)).Token));
        }

        [Fact]
        public async Task Should_handle_two_producers_participating_in_a_single_transaction()
        {
            await using var connection = await CreateConnection();
            var address1 = Guid.NewGuid().ToString();
            var address2 = Guid.NewGuid().ToString();
            await using var producer1 = await connection.CreateProducerAsync(address1, AddressRoutingType.Anycast);
            await using var producer2 = await connection.CreateProducerAsync(address2, AddressRoutingType.Anycast);
            await using var consumer1 = await connection.CreateConsumerAsync(address1, QueueRoutingType.Anycast);
            await using var consumer2 = await connection.CreateConsumerAsync(address2, QueueRoutingType.Anycast);

            await using var transaction = new Transaction();
            await producer1.SendAsync(new Message("foo1"), transaction);
            await producer2.SendAsync(new Message("foo2"), transaction);

            await Task.WhenAll(new Task[]
            {
                Assert.ThrowsAsync<OperationCanceledException>(async () => await consumer1.ReceiveAsync(new CancellationTokenSource(TimeSpan.FromMilliseconds(500)).Token)),
                Assert.ThrowsAsync<OperationCanceledException>(async () => await consumer2.ReceiveAsync(new CancellationTokenSource(TimeSpan.FromMilliseconds(500)).Token))
            });

            await transaction.CommitAsync();

            var msg1 = await consumer1.ReceiveAsync(CancellationToken);
            var msg2 = await consumer2.ReceiveAsync(CancellationToken);

            Assert.Equal("foo1", msg1.GetBody<string>());
            Assert.Equal("foo2", msg2.GetBody<string>());
        }

        [Fact]
        public async Task Should_redeliver_accepted_messages_when_transaction_rolled_back()
        {
            await using var connection = await CreateConnection();
            var address = Guid.NewGuid().ToString();
            await using var producer = await connection.CreateProducerAsync(address, AddressRoutingType.Anycast);
            await using var consumer = await connection.CreateConsumerAsync(address, QueueRoutingType.Anycast);

            await producer.SendAsync(new Message("foo1"));
            await producer.SendAsync(new Message("foo2"));

            var msg1 = await consumer.ReceiveAsync(CancellationToken);
            var msg2 = await consumer.ReceiveAsync(CancellationToken);

            await using var transaction = new Transaction();
            await consumer.AcceptAsync(msg1, transaction, CancellationToken);
            await consumer.AcceptAsync(msg2, transaction, CancellationToken);
            await transaction.RollbackAsync();

            msg1 = await consumer.ReceiveAsync(CancellationToken);
            msg2 = await consumer.ReceiveAsync(CancellationToken);

            Assert.Equal("foo1", msg1.GetBody<string>());
            Assert.Equal("foo2", msg2.GetBody<string>());
        }

        [Fact]
        public async Task Should_not_redeliver_accepted_messages_when_transaction_committed()
        {
            await using var connection = await CreateConnection();
            var address = Guid.NewGuid().ToString();
            await using var producer = await connection.CreateProducerAsync(address, AddressRoutingType.Anycast);
            await using var consumer = await connection.CreateConsumerAsync(address, QueueRoutingType.Anycast);

            await producer.SendAsync(new Message("foo1"));
            await producer.SendAsync(new Message("foo2"));

            var msg1 = await consumer.ReceiveAsync(CancellationToken);
            var msg2 = await consumer.ReceiveAsync(CancellationToken);

            await using var transaction = new Transaction();
            await consumer.AcceptAsync(msg1, transaction, CancellationToken);
            await consumer.AcceptAsync(msg2, transaction, CancellationToken);
            await transaction.CommitAsync();

            await Assert.ThrowsAsync<OperationCanceledException>(async () => await consumer.ReceiveAsync(new CancellationTokenSource(TimeSpan.FromMilliseconds(500)).Token));
        }

        [Fact]
        public async Task Should_deliver_message_when_transaction_committed_for_accept_and_send()
        {
            await using var connection = await CreateConnection();
            var address1 = Guid.NewGuid().ToString();
            var address2 = Guid.NewGuid().ToString();
            await using var producer1 = await connection.CreateProducerAsync(address1, AddressRoutingType.Anycast);
            await using var producer2 = await connection.CreateProducerAsync(address2, AddressRoutingType.Anycast);
            await using var consumer1 = await connection.CreateConsumerAsync(address1, QueueRoutingType.Anycast);
            await using var consumer2 = await connection.CreateConsumerAsync(address2, QueueRoutingType.Anycast);

            await producer1.SendAsync(new Message("foo1"));
            var msg1 = await consumer1.ReceiveAsync(CancellationToken);

            await using var transaction = new Transaction();
            await producer2.SendAsync(new Message("foo2"), transaction);
            await consumer1.AcceptAsync(msg1, transaction);

            await Assert.ThrowsAsync<OperationCanceledException>(async () => await consumer2.ReceiveAsync(new CancellationTokenSource(TimeSpan.FromMilliseconds(500)).Token));

            await transaction.CommitAsync();

            var msg2 = await consumer2.ReceiveAsync(CancellationToken);

            Assert.Equal("foo2", msg2.GetBody<string>());
        }

        [Fact]
        public async Task Should_not_be_possible_to_commit_the_same_transaction_twice()
        {
            await using var connection = await CreateConnection();
            var address = Guid.NewGuid().ToString();
            await using var producer = await connection.CreateProducerAsync(address, AddressRoutingType.Anycast);

            await using var transaction = new Transaction();
            await producer.SendAsync(new Message("foo1"), transaction);

            await transaction.CommitAsync();

            await Assert.ThrowsAsync<InvalidOperationException>(() => transaction.CommitAsync());
        }

        [Fact]
        public async Task Should_not_be_possible_to_roll_back_the_same_transaction_twice()
        {
            await using var connection = await CreateConnection();
            var address = Guid.NewGuid().ToString();
            await using var producer = await connection.CreateProducerAsync(address, AddressRoutingType.Anycast);

            await using var transaction = new Transaction();
            await producer.SendAsync(new Message("foo1"), transaction);

            await transaction.RollbackAsync();

            await Assert.ThrowsAsync<InvalidOperationException>(() => transaction.RollbackAsync());
        }

        [Fact]
        public async Task Should_not_be_possible_to_commit_rolled_back_transaction()
        {
            await using var connection = await CreateConnection();
            var address = Guid.NewGuid().ToString();
            await using var producer = await connection.CreateProducerAsync(address, AddressRoutingType.Anycast);

            await using var transaction = new Transaction();
            await producer.SendAsync(new Message("foo1"), transaction);

            await transaction.RollbackAsync();

            await Assert.ThrowsAsync<InvalidOperationException>(() => transaction.CommitAsync());
        }

        [Fact]
        public async Task Should_not_be_possible_to_roll_back_committed_transaction()
        {
            await using var connection = await CreateConnection();
            var address = Guid.NewGuid().ToString();
            await using var producer = await connection.CreateProducerAsync(address, AddressRoutingType.Anycast);

            await using var transaction = new Transaction();
            await producer.SendAsync(new Message("foo1"), transaction);

            await transaction.CommitAsync();

            await Assert.ThrowsAsync<InvalidOperationException>(() => transaction.RollbackAsync());
        }

        [Fact]
        public async Task Should_rollback_transaction_on_dispose_when_not_committed()
        {
            await using var connection = await CreateConnection();
            var address = Guid.NewGuid().ToString();
            await using var producer = await connection.CreateProducerAsync(address, AddressRoutingType.Anycast);
            await using var consumer = await connection.CreateConsumerAsync(address, QueueRoutingType.Anycast);

            var transaction = new Transaction();
            await producer.SendAsync(new Message("foo1"), transaction);
            await transaction.DisposeAsync();

            await Assert.ThrowsAsync<InvalidOperationException>(() => transaction.CommitAsync());
        }

        [Fact]
        public async Task Should_not_send_more_messages_using_committed_transaction()
        {
            await using var connection = await CreateConnection();
            var address = Guid.NewGuid().ToString();
            await using var producer = await connection.CreateProducerAsync(address, AddressRoutingType.Anycast);

            var transaction = new Transaction();
            await producer.SendAsync(new Message("foo1"), transaction);
            await transaction.CommitAsync();
            await Assert.ThrowsAsync<InvalidOperationException>(() => producer.SendAsync(new Message("foo2"), transaction));
        }

        [Fact]
        public async Task Should_not_send_more_messages_using_rolled_back_transaction()
        {
            await using var connection = await CreateConnection();
            var address = Guid.NewGuid().ToString();
            await using var producer = await connection.CreateProducerAsync(address, AddressRoutingType.Anycast);

            var transaction = new Transaction();
            await producer.SendAsync(new Message("foo1"), transaction);
            await transaction.RollbackAsync();
            await Assert.ThrowsAsync<InvalidOperationException>(() => producer.SendAsync(new Message("foo2"), transaction));
        }

        [Fact]
        public async Task Should_not_accept_more_messages_using_committed_transaction()
        {
            await using var connection = await CreateConnection();
            var address = Guid.NewGuid().ToString();
            await using var producer = await connection.CreateProducerAsync(address, AddressRoutingType.Anycast);
            await using var consumer = await connection.CreateConsumerAsync(address, QueueRoutingType.Anycast);

            await producer.SendAsync(new Message("foo1"));
            await producer.SendAsync(new Message("foo2"));

            var transaction = new Transaction();
            var msg1 = await consumer.ReceiveAsync(CancellationToken);
            await consumer.AcceptAsync(msg1, transaction);
            await transaction.CommitAsync();

            var msg2 = await consumer.ReceiveAsync(CancellationToken);

            await Assert.ThrowsAsync<InvalidOperationException>(async () => await consumer.AcceptAsync(msg2, transaction));
        }

        [Fact]
        public async Task Should_not_accept_more_messages_using_rolled_back_transaction()
        {
            await using var connection = await CreateConnection();
            var address = Guid.NewGuid().ToString();
            await using var producer = await connection.CreateProducerAsync(address, AddressRoutingType.Anycast);
            await using var consumer = await connection.CreateConsumerAsync(address, QueueRoutingType.Anycast);

            await producer.SendAsync(new Message("foo1"));

            var transaction = new Transaction();
            var msg1 = await consumer.ReceiveAsync(CancellationToken);
            await consumer.AcceptAsync(msg1, transaction);
            await transaction.RollbackAsync();

            var msg2 = await consumer.ReceiveAsync(CancellationToken);

            await Assert.ThrowsAsync<InvalidOperationException>(async () => await consumer.AcceptAsync(msg2, transaction));
        }

        [Fact]
        public async Task Should_send_transacted_messages_to_different_addresses_using_anonymous_producer()
        {
            await using var connection = await CreateConnection();
            var address1 = Guid.NewGuid().ToString();
            var address2 = Guid.NewGuid().ToString();
            await using var producer = await connection.CreateAnonymousProducer();
            await using var consumer1 = await connection.CreateConsumerAsync(address1, QueueRoutingType.Anycast);
            await using var consumer2 = await connection.CreateConsumerAsync(address2, QueueRoutingType.Anycast);

            var transaction = new Transaction();
            await producer.SendAsync(address1, AddressRoutingType.Anycast, new Message("foo1"), transaction);
            await producer.SendAsync(address2, AddressRoutingType.Anycast, new Message("foo2"), transaction);
            
            await Task.WhenAll(new Task[]
            {
                Assert.ThrowsAsync<OperationCanceledException>(async () => await consumer1.ReceiveAsync(new CancellationTokenSource(TimeSpan.FromMilliseconds(500)).Token)),
                Assert.ThrowsAsync<OperationCanceledException>(async () => await consumer2.ReceiveAsync(new CancellationTokenSource(TimeSpan.FromMilliseconds(500)).Token))
            });

            await transaction.CommitAsync();

            var msg1 = await consumer1.ReceiveAsync(CancellationToken);
            var msg2 = await consumer2.ReceiveAsync(CancellationToken);
            
            Assert.NotNull(msg1);
            Assert.NotNull(msg2);
        }

        [Fact]
        public async Task Should_send_transacted_and_non_transacted_messages_alternately()
        {
            await using var connection = await CreateConnection();
            var address = Guid.NewGuid().ToString();
            await using var producer = await connection.CreateProducerAsync(address, AddressRoutingType.Anycast);
            await using var consumer = await connection.CreateConsumerAsync(address, QueueRoutingType.Anycast);

            var transaction = new Transaction();
            await producer.SendAsync(new Message("foo1"), transaction);
            await producer.SendAsync(new Message("foo2"), transaction);
            await producer.SendAsync(new Message("foo3"));
            await producer.SendAsync(new Message("foo4"), transaction);
            
            var msg3 = await consumer.ReceiveAsync(CancellationToken);
            Assert.Equal("foo3", msg3.GetBody<string>());
            
            await transaction.CommitAsync();

            var msg1 = await consumer.ReceiveAsync(CancellationToken);
            var msg2 = await consumer.ReceiveAsync(CancellationToken);
            var msg4 = await consumer.ReceiveAsync(CancellationToken);
            
            Assert.Equal("foo1", msg1.GetBody<string>());
            Assert.Equal("foo2", msg2.GetBody<string>());
            Assert.Equal("foo4", msg4.GetBody<string>());
        }
    }
}