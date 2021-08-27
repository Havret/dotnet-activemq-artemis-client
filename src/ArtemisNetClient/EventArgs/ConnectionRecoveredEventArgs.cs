using System;

namespace ActiveMQ.Artemis.Client
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