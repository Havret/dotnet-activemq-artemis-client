using System;

namespace ActiveMQ.Net.InternalUtilities
{
    internal static class DateTimeExtensions
    {
        public static DateTime DropTicsPrecision(this DateTime dateTime)
        {
            // drop tics precision, as AMQP timestamp is represented as milliseconds from Unix epoch
            return new DateTime(dateTime.Ticks / TimeSpan.TicksPerMillisecond  * TimeSpan.TicksPerMillisecond, DateTimeKind.Utc);
        }

        public static long ToUnixTimeMilliseconds(this DateTime dateTime)
        {
            return new DateTimeOffset(dateTime).ToUnixTimeMilliseconds();
        }
        
        public static DateTime FromUnixTimeMilliseconds(long unixTimeMilliseconds)
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(unixTimeMilliseconds).DateTime;
        }
    }
}