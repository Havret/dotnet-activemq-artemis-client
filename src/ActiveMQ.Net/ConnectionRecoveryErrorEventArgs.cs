using System;

namespace ActiveMQ.Net
{
    public class ConnectionRecoveryErrorEventArgs : EventArgs
    {
        public ConnectionRecoveryErrorEventArgs(Exception exception)
        {
            Exception = exception;
        }

        public Exception Exception { get; }
    }
}