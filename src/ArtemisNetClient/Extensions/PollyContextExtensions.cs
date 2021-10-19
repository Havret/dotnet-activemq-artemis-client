using Polly;

namespace ActiveMQ.Artemis.Client
{
    internal static class PollyContextExtensions
    {
        private static string _retryCountKey = "retryCount";
        private static string _endpointKey = "endpoint";

        public static void SetRetryCount(this Context context, int retryCount)
        {
            context[_retryCountKey] = retryCount;
        }

        public static int GetRetryCount(this Context context)
        {
            return (int) context[_retryCountKey];
        }
        
        public static void SetEndpoint(this Context context, Endpoint endpoint)
        {
            context[_endpointKey] = endpoint;
        }

        public static Endpoint GetEndpoint(this Context context)
        {
            return (Endpoint) context[_endpointKey];
        }
    }
}