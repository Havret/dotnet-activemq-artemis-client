using System;
using Polly;

namespace ActiveMQ.Net
{
    internal static class PollyContextExtensions
    {
        private static string _retryCountKey = "retryCount";
        private static string _reconnectDelayKey = "reconnectDelay";

        public static void SetRetryCount(this Context context, int retryCount)
        {
            context[_retryCountKey] = retryCount;
        }

        public static int GetRetryCount(this Context context)
        {
            return (int) context[_retryCountKey];
        }

        public static TimeSpan GetReconnectDelay(this Context context, TimeSpan initialReconnectDelay)
        {
            if (context.TryGetValue(_reconnectDelayKey, out var reconnectDelay))
            {
                return (TimeSpan) reconnectDelay;
            }
            else
            {
                return initialReconnectDelay;
            }
        }

        public static void SetReconnectDelay(this Context context, TimeSpan reconnectDelay)
        {
            context[_reconnectDelayKey] = reconnectDelay;
        }
    }
}