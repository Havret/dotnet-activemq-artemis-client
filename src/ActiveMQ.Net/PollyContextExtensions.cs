using Polly;

namespace ActiveMQ.Net
{
    internal static class PollyContextExtensions
    {
        private static string _retryCountKey = "retryCount";

        public static void SetRetryCount(this Context context, int retryCount)
        {
            context[_retryCountKey] = retryCount;
        }

        public static int GetRetryCount(this Context context)
        {
            return (int) context[_retryCountKey];
        }
    }
}