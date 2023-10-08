using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace ActiveMQ.Artemis.Client;

/// <summary>
/// Contains the TLS/SSL settings for a connection.
/// </summary>
public class SslSettings
{
    internal SslSettings()
    {
    }
    
    /// <summary>
    /// Client certificates to use for mutual authentication.
    /// </summary>
    public X509CertificateCollection ClientCertificates
    {
        get;
        set;
    }
    
    /// <summary>
    /// Supported protocols to use.
    /// </summary>
    public SslProtocols Protocols
    {
        get;
        set;
    }
    
    /// <summary>
    /// Specifies whether certificate revocation should be performed during handshake.
    /// </summary>
    public bool CheckCertificateRevocation
    {
        get;
        set;
    }
    
    /// <summary>
    /// Gets or sets a certificate validation callback to validate remote certificate.
    /// </summary>
    public RemoteCertificateValidationCallback RemoteCertificateValidationCallback
    {
        get;
        set;
    }
    
    /// <summary>
    /// Gets or sets a local certificate selection callback to select the certificate which should be used for authentication.
    /// </summary>
    public LocalCertificateSelectionCallback LocalCertificateSelectionCallback
    {
        get;
        set;
    }
}