﻿using System;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Exceptions;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;

namespace ActiveMQ.Artemis.Client.AutoRecovering
{
    internal abstract class AutoRecoveringProducerBase : IRecoverable
    {
        protected readonly ILogger Logger;
        private readonly AsyncManualResetEvent _manualResetEvent = new AsyncManualResetEvent(true);
        private bool _closed;
        private Exception _failureCause;

        protected AutoRecoveringProducerBase(ILoggerFactory loggerFactory)
        {
            Logger = loggerFactory.CreateLogger(GetType());
        }

        public void Resume()
        {
            var wasSuspended = IsSuspended();
            _manualResetEvent.Set();
            
            if (wasSuspended)
            {
                Log.ProducerResumed(Logger);    
            }
        }

        private bool IsSuspended()
        {
            return !_manualResetEvent.IsSet;
        }

        public async Task RecoverAsync(IConnection connection, CancellationToken cancellationToken)
        {
            var underlyingResource = UnderlyingResource;
            await RecoverUnderlyingProducer(connection, cancellationToken).ConfigureAwait(false);
            await DisposeResourceSafe(underlyingResource).ConfigureAwait(false);
            Log.ProducerRecovered(Logger);
        }

        protected void HandleProducerClosed()
        {
            Suspend();
            RecoveryRequested?.Invoke();
        }

        public void Suspend()
        {
            var wasSuspended = IsSuspended();
            _manualResetEvent.Reset();

            if (!wasSuspended)
            {
                Log.ProducerSuspended(Logger);                
            }
        }

        protected void Wait(CancellationToken cancellationToken)
        {
            _manualResetEvent.Wait(cancellationToken);
        }

        protected Task WaitAsync(CancellationToken cancellationToken)
        {
            return _manualResetEvent.WaitAsync(cancellationToken);
        }

        public event Closed Closed;
        public event RecoveryRequested RecoveryRequested;
        
        public async Task TerminateAsync(Exception exception)
        {
            _closed = true;
            _failureCause = exception;
            _manualResetEvent.Set();
            await DisposeResourceSafe(UnderlyingResource).ConfigureAwait(false);
        }

        public async ValueTask DisposeAsync()
        {
            await DisposeResource(UnderlyingResource).ConfigureAwait(false);
            Closed?.Invoke(this);
        }

        private static async ValueTask DisposeResourceSafe(IAsyncDisposable disposable)
        {
            try
            {
                await DisposeResource(disposable).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private static async ValueTask DisposeResource(IAsyncDisposable disposable)
        {
            if (disposable != null)
            {
                await disposable.DisposeAsync().ConfigureAwait(false);
            }
        }

        protected void CheckState()
        {
            if (_closed)
            {
                if (_failureCause != null)
                {
                    throw new ProducerClosedException(_failureCause);
                }
                else
                {
                    throw new ProducerClosedException();
                }
            }
        }

        protected abstract IAsyncDisposable UnderlyingResource { get; }
        protected abstract Task RecoverUnderlyingProducer(IConnection connection, CancellationToken cancellationToken);

        protected static class Log
        {
            private static readonly Action<ILogger, Exception> _retryingProduceAsync = LoggerMessage.Define(
                LogLevel.Trace,
                0,
                "Retrying send after Producer reestablished.");

            private static readonly Action<ILogger, Exception> _producerRecovered = LoggerMessage.Define(
                LogLevel.Trace,
                0,
                "Producer recovered.");

            private static readonly Action<ILogger, Exception> _producerSuspended = LoggerMessage.Define(
                LogLevel.Trace,
                0,
                "Producer suspended.");

            private static readonly Action<ILogger, Exception> _producerResumed = LoggerMessage.Define(
                LogLevel.Trace,
                0,
                "Producer resumed.");

            public static void RetryingSendAsync(ILogger logger)
            {
                if (logger.IsEnabled(LogLevel.Trace))
                {
                    _retryingProduceAsync(logger, null);
                }
            }

            public static void ProducerRecovered(ILogger logger)
            {
                if (logger.IsEnabled(LogLevel.Trace))
                {
                    _producerRecovered(logger, null);
                }
            }

            public static void ProducerSuspended(ILogger logger)
            {
                if (logger.IsEnabled(LogLevel.Trace))
                {
                    _producerSuspended(logger, null);
                }
            }

            public static void ProducerResumed(ILogger logger)
            {
                if (logger.IsEnabled(LogLevel.Trace))
                {
                    _producerResumed(logger, null);
                }
            }
        }
    }
}