using System;

namespace ActiveMQ.Artemis.Client
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