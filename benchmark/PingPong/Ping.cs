using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client;

namespace PingPong
{
    public class Ping : IAsyncDisposable
    {
        private readonly IConnection _connection;
        private readonly IProducer _producer;
        private readonly IConsumer _consumer;
        private readonly Stopwatch _stopwatch;
        private int _numberOfMessages;
        private int _skipMessages;
        private int _counter;
        private readonly Message _pingMessage;
        private TaskCompletionSource<Stats> _tsc;
        private readonly Task _consumerLoopTask;
        private readonly CancellationTokenSource _cts;

        private Ping(IConnection connection, IProducer producer, IConsumer consumer)
        {
            _connection = connection;
            _producer = producer;
            _consumer = consumer;
            _stopwatch = new Stopwatch();
            _pingMessage = new Message("Ping");
            _cts = new CancellationTokenSource();

            _consumerLoopTask = ConsumerLoop();
        }

        public static async Task<Ping> CreateAsync(Endpoint endpoint)
        {
            var connectionFactory = new ConnectionFactory();
            var connection = await connectionFactory.CreateAsync(endpoint);
            var producer = await connection.CreateProducerAsync("ping", RoutingType.Multicast);
            var consumer = await connection.CreateConsumerAsync("pong", RoutingType.Multicast);
            return new Ping(connection, producer, consumer);
        }

        public Task<Stats> Start(int numberOfMessages, int skipMessages)
        {
            _numberOfMessages = numberOfMessages;
            _skipMessages = skipMessages;
            _stopwatch.Start();
            _tsc = new TaskCompletionSource<Stats>();
            _producer.SendAsync(_pingMessage);
            return _tsc.Task;
        }

        private Task ConsumerLoop()
        {
            return Task.Run(async () =>
            {
                try
                {
                    while (!_cts.IsCancellationRequested)
                    {
                        var msg = await _consumer.ReceiveAsync(_cts.Token);
                        if (_skipMessages > 0)
                            _skipMessages--;
                        else
                            _counter++;

                        if (_counter == _numberOfMessages)
                        {
                            _stopwatch.Stop();
                            _tsc.TrySetResult(new Stats { MessagesCount = _counter, Elapsed = _stopwatch.Elapsed });
                        }
                        else
                        {
                            await _producer.SendAsync(_pingMessage, _cts.Token);
                        }
                        await _consumer.AcceptAsync(msg);
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