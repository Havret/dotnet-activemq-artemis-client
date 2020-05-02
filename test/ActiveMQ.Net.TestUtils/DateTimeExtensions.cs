using System;

namespace ActiveMQ.Net.TestUtils
{
    public static class DateTimeExtensions
    {
        public static DateTime DropTicsPrecision(this DateTime dateTime)
        {
            // drop tics precision, as AMQP timestamp is represented as milliseconds from Unix epoch
            const long ticksPerMillisecond = 10000;
            return new DateTime(DateTime.UtcNow.Ticks / ticksPerMillisecond * ticksPerMillisecond, DateTimeKind.Utc);
        }
    }
}