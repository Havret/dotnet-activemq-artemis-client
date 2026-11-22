using System;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ActiveMQ.Artemis.Client.Extensions.DependencyInjection
{
    internal class ActiveMqConsumer
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ContextualReceiveObservable _receiveObservable;
        private readonly Func<CancellationToken, Task<IConsumer>> _consumerFactory;
        private readonly Func<Message, IConsumer, IServiceProvider, CancellationToken, Task> _handler;
        private Task _task;
        private CancellationTokenSource _cts;
        private readonly ILogger<ActiveMqConsumer> _logger;
        private IConsumer _consumer;

        public ActiveMqConsumer(IServiceProvider serviceProvider,
            ContextualReceiveObservable receiveObservable,
            Func<CancellationToken, Task<IConsumer>> consumerFactory,
            Func<Message, IConsumer, IServiceProvider, CancellationToken, Task> handler)
        {
            _serviceProvider = serviceProvider;
            _receiveObservable = receiveObservable;
            _logger = serviceProvider.GetService<ILogger<ActiveMqConsumer>>();
            _consumerFactory = consumerFactory;
            _handler = handler;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var token = _cts.Token;
            _consumer = await _consumerFactory(cancellationToken).ConfigureAwait(false);
            _task = Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        Message msg;
                        try
                        {
                            msg = await _consumer.ReceiveAsync(token).ConfigureAwait(false);
                        }
                        // The consumer was closed for good, or disposed along with the connection, any further receive would fail again
                        catch (Exception exception) when (exception is ConsumerClosedException or ObjectDisposedException)
                        {
                            _logger.LogError(exception, "Consumer was closed. No more messages will be received.");
                            return;
                        }

                        _receiveObservable.PreReceive(msg);
                        await _handler(msg, _consumer, _serviceProvider, token).ConfigureAwait(false);
                        _receiveObservable.PostReceive(msg);
                    }
                    catch (OperationCanceledException)
                    {
                    }
                    catch (Exception exception)
                    {
                        _logger.LogError(exception, "Exception occurred during message processing.");
                    }
                }
            }, token);
        }

        public async Task StopAsync()
        {
            try
            {
                _cts.Cancel();
                await _task.ConfigureAwait(false);
                _cts.Dispose();
                await _consumer.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception occured during consumer stopping.");
            }
        }
    }
}