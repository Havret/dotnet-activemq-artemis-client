namespace ActiveMQ.Artemis.Client
{
    /// <summary>
    /// Represents the protocol schemes used for AMQP connections.
    /// </summary>
    public enum Scheme
    {
        /// <summary>
        /// Represents the standard AMQP protocol without security.
        /// </summary>
        Amqp,
        
        /// <summary>
        /// Represents the standard AMQP protocol secured with SSL/TLS.
        /// </summary>
        Amqps,
        
        /// <summary>
        /// Represents the AMQP protocol over WebSocket without security.
        /// </summary>
        Ws,
        
        /// <summary>
        /// Represents the AMQP protocol over WebSocket secured with SSL/TLS.
        /// </summary>
        Wss
    }
}