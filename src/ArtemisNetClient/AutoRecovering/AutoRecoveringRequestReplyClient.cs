using System;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Exceptions;
using Amqp;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;

namespace ActiveMQ.Artemis.Client.AutoRecovering;

internal class AutoRecoveringRequestReplyClient : IRequestReplyClient, IRecoverable
{
    private readonly RequestReplyClientConfiguration _configuration;
    private readonly AsyncManualResetEvent _manualResetEvent = new(true);

    // Makes replacing the client mutually exclusive with suspending or terminating on its failure, and with disposal
    private readonly object _syncRoot = new();
    private volatile bool _closed;
    private volatile bool _disposed;
    private volatile Exception _failureCause;
    private volatile IRequestReplyClient _requestReplyClient;
    private readonly ILogger<AutoRecoveringRequestReplyClient> _logger;

    public AutoRecoveringRequestReplyClient(ILoggerFactory loggerFactory, RequestReplyClientConfiguration configuration)
    {
        _configuration = configuration;
        _logger = loggerFactory.CreateLogger<AutoRecoveringRequestReplyClient>();
    }

    public async Task<Message> SendAsync(string address, RoutingType? routingType, Message message, CancellationToken cancellationToken)
    {
        while (true)
        {
            CheckState();
            var requestReplyClient = _requestReplyClient;

            try
            {
                return await requestReplyClient.SendAsync(address, routingType, message, cancellationToken).ConfigureAwait(false);
            }
            catch (RequestReplyClientClosedException e)
            {
                if (IsTerminal(e) && await TryTerminateAsync(requestReplyClient, e).ConfigureAwait(false))
                {
                    // The client cannot be recovered for terminal broker-side close reasons.
                    throw;
                }

                CheckState();
                RequestRecovery(requestReplyClient);

                if (e.RequestSent)
                {
                    // The request may have been processed already, sending it again could make it be processed twice.
                    // Leave it up to the caller.
                    throw new RequestReplyClientClosedException(
                        "The RequestReplyClient was closed before the reply arrived. The request may have been processed.",
                        e.ErrorCode, e);
                }

                Log.RetryingSendAsync(_logger);
                await _manualResetEvent.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (ObjectDisposedException) when (!ReferenceEquals(requestReplyClient, _requestReplyClient))
            {
                // The client was replaced and disposed by the recovery before the request was sent, wait until the
                // replacement is resumed
                CheckState();
                Log.RetryingSendAsync(_logger);
                await _manualResetEvent.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Suspends the client and requests its recovery, unless the failed client has already been replaced. A late
    /// failure of a replaced client, e.g. of a request that was pending on it, must not suspend its healthy
    /// replacement, as that would make it be replaced as well, failing the requests already sent over it.
    /// </summary>
    private void RequestRecovery(IRequestReplyClient failedClient)
    {
        lock (_syncRoot)
        {
            if (_closed || _disposed || !ReferenceEquals(failedClient, _requestReplyClient))
            {
                return;
            }

            Suspend();
        }

        RecoveryRequested?.Invoke();
    }

    private static bool IsTerminal(RequestReplyClientClosedException exception)
    {
        // Unlike for a producer, not found isn't terminal, the client sends to any address and replies to a temporary one
        return exception.ErrorCode is ErrorCode.UnauthorizedAccess;
    }

    private void CheckState()
    {
        if (_closed)
        {
            if (_failureCause != null)
            {
                throw new RequestReplyClientClosedException("The RequestReplyClient was closed due to an unrecoverable error.", _failureCause);
            }
            else
            {
                throw new RequestReplyClientClosedException();
            }
        }

        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(AutoRecoveringRequestReplyClient));
        }
    }

    public async Task RecoverAsync(IConnection connection, CancellationToken cancellationToken)
    {
        var oldRpcClient = _requestReplyClient;
        var newRpcClient = await connection.CreateRequestReplyClientAsync(_configuration, cancellationToken).ConfigureAwait(false);
        lock (_syncRoot)
        {
            if (!_closed && !_disposed)
            {
                _requestReplyClient = newRpcClient;
                newRpcClient = null;
            }
        }

        if (newRpcClient != null)
        {
            // Terminated or disposed in the meantime, nobody is going to use it
            await DisposeUnderlyingRpcClientSafe(newRpcClient).ConfigureAwait(false);
            return;
        }

        await DisposeUnderlyingRpcClientSafe(oldRpcClient).ConfigureAwait(false);
        if (oldRpcClient == null)
        {
            Log.RequestReplyClientCreated(_logger);
        }
        else
        {
            Log.RequestReplyClientRecovered(_logger);
        }
    }

    public void Suspend()
    {
        lock (_syncRoot)
        {
            // Nothing is going to resume it anymore, the requests waiting for it would wait forever
            if (_closed || _disposed)
            {
                return;
            }

            var wasSuspended = IsSuspended;
            _manualResetEvent.Reset();

            if (!wasSuspended)
            {
                Log.RequestReplyClientSuspended(_logger);
            }
        }
    }

    public void Resume()
    {
        var wasSuspended = IsSuspended;
        _manualResetEvent.Set();

        if (wasSuspended)
        {
            Log.RequestReplyClientResumed(_logger);
        }
    }
    
    public bool IsSuspended => !_manualResetEvent.IsSet;

    public event Closed Closed;

    public event RecoveryRequested RecoveryRequested;

    public async Task TerminateAsync(Exception exception)
    {
        lock (_syncRoot)
        {
            MarkClosed(exception);
        }

        await DisposeUnderlyingRpcClientSafe(_requestReplyClient).ConfigureAwait(false);
        Closed?.Invoke(this);
    }

    /// <summary>
    /// Terminates the client on a terminal failure, unless the failed client has already been replaced. A late
    /// failure of a replaced client must not terminate its healthy replacement.
    /// </summary>
    /// <returns>True when the client was terminated.</returns>
    private async Task<bool> TryTerminateAsync(IRequestReplyClient failedClient, Exception exception)
    {
        lock (_syncRoot)
        {
            if (!ReferenceEquals(failedClient, _requestReplyClient))
            {
                return false;
            }

            MarkClosed(exception);
        }

        await DisposeUnderlyingRpcClientSafe(failedClient).ConfigureAwait(false);
        Closed?.Invoke(this);
        return true;
    }

    private void MarkClosed(Exception exception)
    {
        _failureCause = exception;
        _closed = true;
        _manualResetEvent.Set();
    }

    public async ValueTask DisposeAsync()
    {
        lock (_syncRoot)
        {
            _disposed = true;
        }

        // Wake up the requests waiting for the recovery, it's not going to happen anymore
        _manualResetEvent.Set();

        await DisposeUnderlyingRpcClient(_requestReplyClient).ConfigureAwait(false);
        Closed?.Invoke(this);
    }

    private async Task DisposeUnderlyingRpcClientSafe(IRequestReplyClient requestReplyClient)
    {
        try
        {
            await DisposeUnderlyingRpcClient(requestReplyClient).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // ignored
        }
    }

    private static async ValueTask DisposeUnderlyingRpcClient(IRequestReplyClient requestReplyClient)
    {
        if (requestReplyClient != null)
        {
            await requestReplyClient.DisposeAsync().ConfigureAwait(false);
        }
    }

    private static class Log
    {
        private static readonly Action<ILogger, Exception> _retryingSendAsync = LoggerMessage.Define(
            LogLevel.Warning,
            0,
            "Retrying send after RequestReplyClient reestablished.");

        private static readonly Action<ILogger, Exception> _requestReplyClientCreated = LoggerMessage.Define(
            LogLevel.Information,
            0,
            "RequestReplyClient created.");

        private static readonly Action<ILogger, Exception> _requestReplyClientRecovered = LoggerMessage.Define(
            LogLevel.Information,
            0,
            "RequestReplyClient recovered.");

        private static readonly Action<ILogger, Exception> _requestReplyClientClientSuspended = LoggerMessage.Define(
            LogLevel.Warning,
            0,
            "RequestReplyClient suspended.");

        private static readonly Action<ILogger, Exception> _requestReplyClientClientResumed = LoggerMessage.Define(
            LogLevel.Information,
            0,
            "RequestReplyClient resumed.");

        public static void RetryingSendAsync(ILogger logger)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                _retryingSendAsync(logger, null);
            }
        }

        public static void RequestReplyClientCreated(ILogger logger)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                _requestReplyClientCreated(logger, null);
            }
        }

        public static void RequestReplyClientRecovered(ILogger logger)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                _requestReplyClientRecovered(logger, null);
            }
        }

        public static void RequestReplyClientSuspended(ILogger logger)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                _requestReplyClientClientSuspended(logger, null);
            }
        }

        public static void RequestReplyClientResumed(ILogger logger)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                _requestReplyClientClientResumed(logger, null);
            }
        }
    }
}