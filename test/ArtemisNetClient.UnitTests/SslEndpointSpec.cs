using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using Xunit;

namespace ActiveMQ.Artemis.Client.UnitTests
{
    public class SslEndpointSpec
    {
        [Fact]
        public void Should_create_ssl_endpoint()
        {
            var endpoint = Endpoint.Create("localhost", 5672, ClientCertificates(), TrustedRemoteCertificateAuthorities());
            Assert.Equal("localhost", endpoint.Host);
            Assert.Equal(5672, endpoint.Port);
            Assert.NotNull(endpoint.ClientCertificates);
            Assert.NotNull(endpoint.TrustedRemoteCertificateAuthorities);
        }

        [Fact]
        public void Should_create_ssl_endpoint_when_certificates_are_null()
        {
            var endpoint = Endpoint.Create("localhost", 5672);
            Assert.Equal("localhost", endpoint.Host);
            Assert.Equal(5672, endpoint.Port);
            Assert.Null(endpoint.ClientCertificates);
            Assert.Null(endpoint.TrustedRemoteCertificateAuthorities);
        }

        private static X509CertificateCollection TrustedRemoteCertificateAuthorities()
        {
            return new X509CertificateCollection(new List<X509Certificate>()
            {
                new X509Certificate()
            }.ToArray());
        }

        private static X509CertificateCollection ClientCertificates()
        {
            return new X509CertificateCollection(new List<X509Certificate>()
            {
                new X509Certificate()
            }.ToArray());
        }

        [Theory, MemberData(nameof(EndpointData))]
        public void Should_return_string_representation(Endpoint endpoint, string expectedString)
        {
            Assert.Equal(expectedString, endpoint.ToString());
        }

        public static IEnumerable<object[]> EndpointData()
        {
            return new[]
            {
                new object[] { Endpoint.Create("localhost", 5762, ClientCertificates(), TrustedRemoteCertificateAuthorities()), "amqps://localhost:5762" },
            };
        }

    }
}