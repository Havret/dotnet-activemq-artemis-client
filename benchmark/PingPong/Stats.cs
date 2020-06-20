using System;

namespace PingPong
{
    public class Stats
    {
        public int MessagesCount { get; set; }
        public TimeSpan Elapsed { get; set; }

        public override string ToString()
        {
            return $"Sent {MessagesCount} msg in {Elapsed.TotalMilliseconds:F2}ms -- {MessagesCount / Elapsed.TotalSeconds:F2} msg/s";
        }
    }
}