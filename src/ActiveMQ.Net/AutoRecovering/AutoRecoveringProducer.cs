﻿using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Net.Exceptions;
using Microsoft.Extensions.Logging;

namespace ActiveMQ.Net.AutoRecovering
{
    internal class AutoRecoveringProducer : AutoRecoveringProducerBase, IProducer
    {
        private readonly ProducerConfiguration _configuration;
        private IProducer _producer;

        public AutoRecoveringProducer(ILoggerFactory loggerFactory, ProducerConfiguration configuration) : base(loggerFactory)
        {
            _configuration = configuration;
        }

        public async Task SendAsync(Message message, CancellationToken cancellationToken = default)
        {
            try
            {
                await _producer.SendAsync(message, cancellationToken).ConfigureAwait(false);
            }
            catch (ProducerClosedException)
            {
                Suspend();
                await WaitAsync(cancellationToken).ConfigureAwait(false);
                Log.RetryingSendAsync(Logger);
                await _producer.SendAsync(message, cancellationToken).ConfigureAwait(false);
            }
        }

        public void Send(Message message)
        {
            try
            {
                _producer.Send(message);
            }
            catch (ProducerClosedException)
            {
                Suspend();
                Wait();
                Log.RetryingSendAsync(Logger);
                _producer.Send(message);
            }
        }
        
        protected override async Task RecoverUnderlyingProducer(IConnection connection, CancellationToken cancellationToken)
        {
            _producer = await connection.CreateProducerAsync(_configuration, cancellationToken).ConfigureAwait(false);
        }

        protected override ValueTask DisposeUnderlyingProducer()
        {
            return _producer.DisposeAsync();
        }
    }
}