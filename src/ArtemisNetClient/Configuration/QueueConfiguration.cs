using System;

namespace ActiveMQ.Artemis.Client
{
    public class QueueConfiguration
    {
        public string Name { get; set; }
        public RoutingType RoutingType { get; set; }
        public string Address { get; set; }
        public bool Durable { get; set; } = true;
        public int MaxConsumers { get; set; } = -1;
        public bool Exclusive { get; set; }
        public bool GroupRebalance { get; set; }
        public int GroupBuckets { get; set; } = -1;
        public bool PurgeOnNoConsumers { get; set; }
        public bool AutoCreateAddress { get; set; }
        public string FilterExpression { get; set; }

        /// <summary>
        /// Delete created queues automatically when there are no consumers attached.
        /// </summary>
        public bool AutoDelete { get; set; } = false;

        /// <summary>
        /// The message count that the queue must be less than or equal to before auto-deleting it.
        /// To disable the message count check, -1 can be set. Default is 0 (empty queue).
        /// </summary>
        public int AutoDeleteMessageCount { get; set; }

        /// <summary>
        /// Delay for auto-deleting queues that do not have any consumers attached.
        /// </summary>
        public TimeSpan? AutoDeleteDelay { get; set; }
    }
}