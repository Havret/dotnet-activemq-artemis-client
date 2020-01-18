using System;

namespace ActiveMQ.Net
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