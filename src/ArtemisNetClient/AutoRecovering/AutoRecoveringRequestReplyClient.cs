using System;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Exceptions;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;

namespace ActiveMQ.Artemis.Client.AutoRecovering;

internal class AutoRecoveringRequestReplyClient : IRequestReplyClient, IRecoverable
{
    private readonly RequestReplyClientConfiguration _configuration;
    private readonly AsyncManualResetEvent _manualResetEvent = new(true);
    private bool _closed;
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

            try
            {
                return await _requestReplyClient.SendAsync(address, routingType, message, cancellationToken).ConfigureAwait(false);
            }
            catch (RequestReplyClientClosedException)
            {
                CheckState();

                Log.RetryingSendAsync(_logger);

                Suspend();
                RecoveryRequested?.Invoke();
                await _manualResetEvent.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
        }
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
    }

    public async Task RecoverAsync(IConnection connection, CancellationToken cancellationToken)
    {
        var oldRpcClient = _requestReplyClient;
        _requestReplyClient = await connection.CreateRequestReplyClientAsync(_configuration, cancellationToken).ConfigureAwait(false);
        await DisposeUnderlyingRpcClientSafe(oldRpcClient).ConfigureAwait(false);
        Log.RequestReplyClientRecovered(_logger);
    }

    public void Suspend()
    {
        var wasSuspended = IsSuspended();
        _manualResetEvent.Reset();

        if (!wasSuspended)
        {
            Log.RequestReplyClientSuspended(_logger);                
        }
    }

    public void Resume()
    {
        var wasSuspended = IsSuspended();
        _manualResetEvent.Set();

        if (wasSuspended)
        {
            Log.RequestReplyClientResumed(_logger);
        }
    }
    
    private bool IsSuspended()
    {
        return !_manualResetEvent.IsSet;
    }

    public event Closed Closed;

    public event RecoveryRequested RecoveryRequested;

    public async Task TerminateAsync(Exception exception)
    {
        _closed = true;
        _failureCause = exception;
        _manualResetEvent.Set();
        await DisposeUnderlyingRpcClientSafe(_requestReplyClient).ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeUnderlyingRpcClient(_requestReplyClient).ConfigureAwait(false);
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
            LogLevel.Trace,
            0,
            "Retrying send after RequestReplyClient reestablished.");

        private static readonly Action<ILogger, Exception> _requestReplyClientRecovered = LoggerMessage.Define(
            LogLevel.Trace,
            0,
            "RequestReplyClient recovered.");

        private static readonly Action<ILogger, Exception> _requestReplyClientClientSuspended = LoggerMessage.Define(
            LogLevel.Trace,
            0,
            "RequestReplyClient suspended.");

        private static readonly Action<ILogger, Exception> _requestReplyClientClientResumed = LoggerMessage.Define(
            LogLevel.Trace,
            0,
            "RequestReplyClient resumed.");

        public static void RetryingSendAsync(ILogger logger)
        {
            if (logger.IsEnabled(LogLevel.Trace))
            {
                _retryingSendAsync(logger, null);
            }
        }

        public static void RequestReplyClientRecovered(ILogger logger)
        {
            if (logger.IsEnabled(LogLevel.Trace))
            {
                _requestReplyClientRecovered(logger, null);
            }
        }

        public static void RequestReplyClientSuspended(ILogger logger)
        {
            if (logger.IsEnabled(LogLevel.Trace))
            {
                _requestReplyClientClientSuspended(logger, null);
            }
        }

        public static void RequestReplyClientResumed(ILogger logger)
        {
            if (logger.IsEnabled(LogLevel.Trace))
            {
                _requestReplyClientClientResumed(logger, null);
            }
        }
    }
}