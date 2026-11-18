using System;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Exceptions;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;

namespace ActiveMQ.Artemis.Client.AutoRecovering
{
    internal abstract class AutoRecoveringProducerBase<TProducer> : IRecoverable where TProducer : class, IAsyncDisposable
    {
        protected readonly ILogger Logger;
        private readonly AsyncManualResetEvent _manualResetEvent = new(true);

        // Makes replacing the producer mutually exclusive with suspending or terminating on its failure, and with disposal
        private readonly object _syncRoot = new();
        private volatile TProducer _producer;
        private volatile bool _closed;
        private volatile bool _disposed;
        private volatile Exception _failureCause;

        protected AutoRecoveringProducerBase(ILoggerFactory loggerFactory)
        {
            Logger = loggerFactory.CreateLogger(GetType());
        }

        public void Resume()
        {
            var wasSuspended = IsSuspended;
            _manualResetEvent.Set();

            if (wasSuspended)
            {
                Log.ProducerResumed(Logger);
            }
        }

        public bool IsSuspended => !_manualResetEvent.IsSet;

        protected TProducer Producer => _producer;

        public async Task RecoverAsync(IConnection connection, CancellationToken cancellationToken)
        {
            var oldProducer = _producer;
            var newProducer = await CreateUnderlyingProducer(connection, cancellationToken).ConfigureAwait(false);
            lock (_syncRoot)
            {
                if (!_closed && !_disposed)
                {
                    _producer = newProducer;
                    newProducer = null;
                }
            }

            if (newProducer != null)
            {
                // Terminated or disposed in the meantime, nobody is going to use it
                await DisposeResourceSafe(newProducer).ConfigureAwait(false);
                return;
            }

            await DisposeResourceSafe(oldProducer).ConfigureAwait(false);
            if (oldProducer == null)
            {
                Log.ProducerCreated(Logger);
            }
            else
            {
                Log.ProducerRecovered(Logger);
            }
        }

        /// <summary>
        /// Suspends the producer and requests its recovery, unless the failed producer has already been replaced.
        /// A late failure of a replaced producer must not suspend its healthy replacement, as that would make it be
        /// replaced as well.
        /// </summary>
        protected void HandleProducerClosed(TProducer failedProducer)
        {
            lock (_syncRoot)
            {
                if (_closed || _disposed || !ReferenceEquals(failedProducer, _producer))
                {
                    return;
                }

                Suspend();
            }

            RecoveryRequested?.Invoke();
        }

        public void Suspend()
        {
            lock (_syncRoot)
            {
                // Nothing is going to resume it anymore, the sends waiting for it would wait forever
                if (_closed || _disposed)
                {
                    return;
                }

                var wasSuspended = IsSuspended;
                _manualResetEvent.Reset();

                if (!wasSuspended)
                {
                    Log.ProducerSuspended(Logger);
                }
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
            lock (_syncRoot)
            {
                MarkClosed(exception);
            }

            await CompleteTermination(exception).ConfigureAwait(false);
        }

        /// <summary>
        /// Terminates the producer on a terminal failure, unless the failed producer has already been replaced.
        /// A late failure of a replaced producer must not terminate its healthy replacement.
        /// </summary>
        /// <returns>True when the producer was terminated.</returns>
        protected async Task<bool> TryTerminateAsync(TProducer failedProducer, Exception exception)
        {
            lock (_syncRoot)
            {
                if (!ReferenceEquals(failedProducer, _producer))
                {
                    return false;
                }

                MarkClosed(exception);
            }

            await CompleteTermination(exception).ConfigureAwait(false);
            return true;
        }

        private void MarkClosed(Exception exception)
        {
            _failureCause = exception;
            _closed = true;
            _manualResetEvent.Set();
        }

        private async Task CompleteTermination(Exception exception)
        {
            Log.ProducerTerminated(Logger, exception);
            await DisposeResourceSafe(_producer).ConfigureAwait(false);
            Closed?.Invoke(this);
        }

        public async ValueTask DisposeAsync()
        {
            lock (_syncRoot)
            {
                _disposed = true;
            }

            // Wake up the sends waiting for the recovery, it's not going to happen anymore
            _manualResetEvent.Set();
            await DisposeResource(_producer).ConfigureAwait(false);
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

            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        protected abstract Task<TProducer> CreateUnderlyingProducer(IConnection connection, CancellationToken cancellationToken);

        protected static class Log
        {
            private static readonly Action<ILogger, string, Exception> _retryingProduceAsync = LoggerMessage.Define<string>(
                LogLevel.Warning,
                0,
                "Retrying send to address {0} after Producer reestablished.");

            private static readonly Action<ILogger, Exception> _producerCreated = LoggerMessage.Define(
                LogLevel.Information,
                0,
                "Producer created.");

            private static readonly Action<ILogger, Exception> _producerRecovered = LoggerMessage.Define(
                LogLevel.Information,
                0,
                "Producer recovered.");

            private static readonly Action<ILogger, Exception> _producerSuspended = LoggerMessage.Define(
                LogLevel.Warning,
                0,
                "Producer suspended.");

            private static readonly Action<ILogger, Exception> _producerResumed = LoggerMessage.Define(
                LogLevel.Information,
                0,
                "Producer resumed.");
            
            private static readonly Action<ILogger, Exception> _producerTerminated = LoggerMessage.Define(
                LogLevel.Error,
                0,
                "Producer terminated.");

            public static void RetryingSendAsync(ILogger logger, string address)
            {
                if (logger.IsEnabled(LogLevel.Warning))
                {
                    _retryingProduceAsync(logger, address, null);
                }
            }

            public static void ProducerCreated(ILogger logger)
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    _producerCreated(logger, null);
                }
            }

            public static void ProducerRecovered(ILogger logger)
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    _producerRecovered(logger, null);
                }
            }

            public static void ProducerSuspended(ILogger logger)
            {
                if (logger.IsEnabled(LogLevel.Warning))
                {
                    _producerSuspended(logger, null);
                }
            }

            public static void ProducerResumed(ILogger logger)
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    _producerResumed(logger, null);
                }
            }
            
            public static void ProducerTerminated(ILogger logger, Exception exception)
            {
                if (logger.IsEnabled(LogLevel.Error))
                {
                    _producerTerminated(logger, exception);
                }
            }
        }
    }
}