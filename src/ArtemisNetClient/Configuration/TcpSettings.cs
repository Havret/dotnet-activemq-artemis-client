namespace ActiveMQ.Artemis.Client;

/// <summary>
/// Contains the TCP settings of a connection.
/// </summary>
public class TcpSettings
{
    /// <summary>
    /// Gets or sets a value in milliseconds that defines how often a keep-alive transmission is sent to an idle connection.
    /// </summary>
    public uint KeepAliveTime { get; set; }

    /// <summary>
    /// Gets or sets a value in milliseconds that defines how often a keep-alive transmission
    /// is sent when no response is received from previous keep-alive transmissions.
    /// </summary>
    public uint KeepAliveInterval { get; set; }
}