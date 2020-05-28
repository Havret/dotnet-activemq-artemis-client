namespace ActiveMQ.Artemis.Client
{
    internal enum TerminusDurability : uint
    {
        /// <summary>
        /// No terminus state is retained durably.
        /// </summary>
        None = 0,

        /// <summary>
        /// Only the existence and configuration of the terminus is retained durably.
        /// </summary>
        Configuration = 1,

        /// <summary>
        /// In addition to the existence and configuration of the terminus, the unsettled state for durable messages is retained durably.
        /// </summary>
        UnsettledState = 2
    }
}