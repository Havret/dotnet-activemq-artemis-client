using System;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Exceptions;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;

namespace ActiveMQ.Artemis.Client.AutoRecovering;

internal class AutoRecoveringRpcClient : IRpcClient, IRecoverable
{
    private readonly AsyncManualResetEvent _manualResetEvent = new(true);
    private bool _closed;
    private volatile Exception _failureCause;
    private volatile IRpcClient _rpcClient;
    private readonly ILogger<AutoRecoveringRpcClient> _logger;

    public AutoRecoveringRpcClient(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<AutoRecoveringRpcClient>();
    }

    public async Task<Message> SendAsync(string address, RoutingType? routingType, Message message, CancellationToken cancellationToken)
    {
        while (true)
        {
            CheckState();

            try
            {
                return await _rpcClient.SendAsync(address, routingType, message, cancellationToken).ConfigureAwait(false);
            }
            catch (RpcClientClosedException)
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
                throw new RpcClientClosedException("The RpcClient was closed due to an unrecoverable error.", _failureCause);
            }
            else
            {
                throw new RpcClientClosedException();
            }
        }
    }

    public async Task RecoverAsync(IConnection connection, CancellationToken cancellationToken)
    {
        var oldRpcClient = _rpcClient;
        _rpcClient = await connection.CreateRpcClientAsync(cancellationToken).ConfigureAwait(false);
        await DisposeUnderlyingRpcClientSafe(oldRpcClient).ConfigureAwait(false);
        Log.RpcClientRecovered(_logger);
    }

    public void Suspend()
    {
        var wasSuspended = IsSuspended();
        _manualResetEvent.Reset();

        if (!wasSuspended)
        {
            Log.RpcClientSuspended(_logger);                
        }
    }

    public void Resume()
    {
        var wasSuspended = IsSuspended();
        _manualResetEvent.Set();

        if (wasSuspended)
        {
            Log.RpcClientResumed(_logger);
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
        await DisposeUnderlyingRpcClientSafe(_rpcClient).ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeUnderlyingRpcClient(_rpcClient).ConfigureAwait(false);
    }

    private async Task DisposeUnderlyingRpcClientSafe(IRpcClient rpcClient)
    {
        try
        {
            await DisposeUnderlyingRpcClient(rpcClient).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // ignored
        }
    }

    private static async ValueTask DisposeUnderlyingRpcClient(IRpcClient rpcClient)
    {
        if (rpcClient != null)
        {
            await rpcClient.DisposeAsync().ConfigureAwait(false);
        }
    }

    private static class Log
    {
        private static readonly Action<ILogger, Exception> _retryingSendAsync = LoggerMessage.Define(
            LogLevel.Trace,
            0,
            "Retrying send after RpcClient reestablished.");

        private static readonly Action<ILogger, Exception> _rpcClientRecovered = LoggerMessage.Define(
            LogLevel.Trace,
            0,
            "RpcClient recovered.");

        private static readonly Action<ILogger, Exception> _rpcClientSuspended = LoggerMessage.Define(
            LogLevel.Trace,
            0,
            "RpcClient suspended.");

        private static readonly Action<ILogger, Exception> _rpcClientResumed = LoggerMessage.Define(
            LogLevel.Trace,
            0,
            "RpcClient resumed.");

        public static void RetryingSendAsync(ILogger logger)
        {
            if (logger.IsEnabled(LogLevel.Trace))
            {
                _retryingSendAsync(logger, null);
            }
        }

        public static void RpcClientRecovered(ILogger logger)
        {
            if (logger.IsEnabled(LogLevel.Trace))
            {
                _rpcClientRecovered(logger, null);
            }
        }

        public static void RpcClientSuspended(ILogger logger)
        {
            if (logger.IsEnabled(LogLevel.Trace))
            {
                _rpcClientSuspended(logger, null);
            }
        }

        public static void RpcClientResumed(ILogger logger)
        {
            if (logger.IsEnabled(LogLevel.Trace))
            {
                _rpcClientResumed(logger, null);
            }
        }
    }
}