using System;
using System.Linq;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Exceptions;
using ActiveMQ.Artemis.Client.MessageIdPolicy;
using Amqp;
using Amqp.Framing;
using Microsoft.Extensions.Logging;

namespace ActiveMQ.Artemis.Client.Builders
{
    internal class ConnectionBuilder
    {
        const uint DefaultMaxFrameSize = 256 * 1024;
        const ushort ChannelMax = 255;

        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<ConnectionBuilder> _logger;
        private readonly Func<IMessageIdPolicy> _messageIdPolicyFactory;
        private readonly Func<string> _clientIdFactory;
        private readonly TaskCompletionSource<bool> _tcs;
        private X509CertificateCollection _trustedRemoteCertificateAuthorities;
        private bool _bypassRemoteCertificateValidation;

        public ConnectionBuilder(ILoggerFactory loggerFactory,
            Func<IMessageIdPolicy> messageIdPolicyFactory,
            Func<string> clientIdFactory)
        {
            _loggerFactory = loggerFactory;
            _logger = _loggerFactory.CreateLogger<ConnectionBuilder>();
            _messageIdPolicyFactory = messageIdPolicyFactory;
            _clientIdFactory = clientIdFactory;
            _tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public async Task<IConnection> CreateAsync(Endpoint endpoint, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var _ = cancellationToken.Register(() => _tcs.TrySetCanceled());

            var connectionFactory = endpoint.Scheme is Scheme.Ws or Scheme.Wss
                ? new Amqp.ConnectionFactory(new[] { new WebSocketTransportFactory() })
                : new Amqp.ConnectionFactory();

            if (endpoint.ClientCertificates != null)
            {
                _trustedRemoteCertificateAuthorities = endpoint.TrustedRemoteCertificateAuthorities;
                _bypassRemoteCertificateValidation = endpoint.BypassRemoteCertificateValidation;

                connectionFactory.SSL.ClientCertificates.AddRange(endpoint.ClientCertificates);
                connectionFactory.SSL.RemoteCertificateValidationCallback = InternalValidateRemoteCertificate;
                connectionFactory.SSL.LocalCertificateSelectionCallback = SelectFirstLocalCertificate;
                connectionFactory.SSL.CheckCertificateRevocation = false;
                // anonymous due use of client certificate
                connectionFactory.SASL.Profile = Amqp.Sasl.SaslProfile.Anonymous;
            }

            try
            {
                var open = GetOpenFrame(endpoint);
                var connection = await connectionFactory.CreateAsync(endpoint.Address, open, OnOpened).ConfigureAwait(false);
                connection.AddClosedCallback(OnClosed);
                await _tcs.Task.ConfigureAwait(false);
                connection.Closed -= OnClosed;
                return new Connection(_loggerFactory, endpoint, connection, _messageIdPolicyFactory);
            }
            catch (AmqpException exception)
            {
                throw new CreateConnectionException(message: exception.Error?.Description ?? exception.Message, errorCode: exception.Error?.Condition);
            }
        }

        private Open GetOpenFrame(Endpoint endpoint)
        {
            if (_clientIdFactory != null)
            {
                return new Open
                {
                    ContainerId = _clientIdFactory(),
                    HostName = endpoint.Host,
                    MaxFrameSize = DefaultMaxFrameSize,
                    ChannelMax = ChannelMax
                };
            }

            return null;
        }

        private void OnOpened(Amqp.IConnection connection, Open open)
        {
            if (connection != null)
            {
                _tcs.TrySetResult(true);
            }
        }

        private void OnClosed(IAmqpObject sender, Error error)
        {
            if (error != null)
            {
                _tcs.TrySetException(new CreateConnectionException(error.Description, error.Condition));
            }
        }
        
        private bool InternalValidateRemoteCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            _logger.LogInformation("Received remote certificate. Subject: {Subject} {Issuer}, Policy errors: {SslPolicyErrors}",
                certificate.Subject,
                certificate.Issuer,
                sslPolicyErrors);

            if (_bypassRemoteCertificateValidation)
            {
                _logger.LogWarning("Bypass validation for remote certificate {Subject} {Issuer}", certificate.Subject, certificate.Issuer);
                return true;
            }

            if (sslPolicyErrors == SslPolicyErrors.None)
            {
                return true;
            }

            if (sslPolicyErrors == SslPolicyErrors.RemoteCertificateChainErrors)
            {
                _logger.LogInformation("Validate Remote Certificate against trusted remote CAs");

                // check certificate in chain against trusted ca
                if (_trustedRemoteCertificateAuthorities == null)
                    return false;

                foreach (var remote in _trustedRemoteCertificateAuthorities)
                {
                    var ca = new X509Certificate2(remote);
                    var hasMatch = chain.ChainElements
                        .Cast<X509ChainElement>()
                        .Any(x => x.Certificate.Thumbprint == ca.Thumbprint);

                    if (hasMatch)
                    {
                        _logger.LogInformation("Found valid CA {CertificateAuthority} for {Certificate} {Issuer}", ca.Subject, certificate.Subject, certificate.Issuer);
                        return true;
                    }
                }

                return false;
            }
            else
            {
                return false;
            }
        }

        private X509Certificate SelectFirstLocalCertificate(object sender, string targetHost, X509CertificateCollection localCertificates, X509Certificate remoteCertificate, string[] acceptableIssuers)
        {
            if (localCertificates.Count == 0)
                return null;

            var first = localCertificates[0];
            return first;
        }
    }
}