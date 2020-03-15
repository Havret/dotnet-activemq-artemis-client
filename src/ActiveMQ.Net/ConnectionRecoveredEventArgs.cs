using System;

namespace ActiveMQ.Net
{
    public class ConnectionRecoveredEventArgs : EventArgs
    {
        public ConnectionRecoveredEventArgs(Endpoint endpoint)
        {
            Endpoint = endpoint;
        }

        public Endpoint Endpoint { get; }
    }
}