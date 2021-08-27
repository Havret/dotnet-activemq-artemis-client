using System;

namespace ActiveMQ.Artemis.Client
{
    public class ConnectionClosedEventArgs : EventArgs
    {
        public ConnectionClosedEventArgs(bool closedByPeer, string error)
        {
            ClosedByPeer = closedByPeer;
            Error = error;
        }

        public bool ClosedByPeer { get; }
        public string Error { get; }
    }
}