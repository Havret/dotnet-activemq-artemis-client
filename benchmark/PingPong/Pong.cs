using System;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client;

namespace PingPong
{
    public class Pong : IAsyncDisposable
    {
        private readonly IConnection _connection;
        private readonly IProducer _producer;
        private readonly IConsumer _consumer;
        private readonly Message _pongMessage;
        private readonly Task _consumerLoopTask;
        private readonly CancellationTokenSource _cts;

        private Pong(IConnection connection, IProducer producer, IConsumer consumer)
        {
            _connection = connection;
            _producer = producer;
            _consumer = consumer;
            _cts = new CancellationTokenSource();
            _pongMessage = new Message("Pong");

            _consumerLoopTask = ConsumerLoop();
        }

        public static async Task<Pong> CreateAsync(Endpoint endpoint)
        {
            var connectionFactory = new ConnectionFactory();
            var connection = await connectionFactory.CreateAsync(endpoint);
            var producer = await connection.CreateProducerAsync("pong", RoutingType.Multicast);
            var consumer = await connection.CreateConsumerAsync("ping", RoutingType.Multicast);
            return new Pong(connection, producer, consumer);
        }

        private Task ConsumerLoop()
        {
            return Task.Run(async () =>
            {
                try
                {
                    while (!_cts.IsCancellationRequested)
                    {
                        var pingMsg = await _consumer.ReceiveAsync(_cts.Token);
                        await _producer.SendAsync(_pongMessage, _cts.Token);
                        await _consumer.AcceptAsync(pingMsg);
                    }
                }
                catch (OperationCanceledException)
                {
                }
            });
        }

        public async ValueTask DisposeAsync()
        {
            _cts.Cancel();
            _cts.Dispose();
            await _consumerLoopTask;
            await _consumer.DisposeAsync();
            await _producer.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}