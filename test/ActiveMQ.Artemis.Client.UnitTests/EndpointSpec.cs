using System.Collections.Generic;
using ActiveMQ.Artemis.Client.Exceptions;
using Amqp;
using Xunit;

namespace ActiveMQ.Artemis.Client.UnitTests
{
    public class EndpointSpec
    {
        [Fact]
        public void Should_create_endpoint()
        {
            var endpoint = Endpoint.Create("localhost", 5672, "guest", "guest");
            Assert.Equal("localhost", endpoint.Host);
            Assert.Equal(5672, endpoint.Port);
            Assert.Equal("guest", endpoint.User);
            Assert.Equal("guest", endpoint.Password);
        }

        [Fact]
        public void Should_create_endpoint_when_only_host_and_port_specified()
        {
            var endpoint = Endpoint.Create("localhost", 5672);
            Assert.Equal("localhost", endpoint.Host);
            Assert.Equal(5672, endpoint.Port);
            Assert.Null(endpoint.User);
            Assert.Null(endpoint.Password);
        }

        [Fact]
        public void Should_create_endpoint_using_AMQP_scheme_when_no_scheme_specified_explicitly()
        {
            var endpoint = Endpoint.Create("localhost", 5672, "guest", "guest");
            Assert.Equal(Scheme.Amqp, endpoint.Scheme);
        }

        [Theory, MemberData(nameof(SchemaData))]
        public void Should_create_endpoint_when_proper_scheme_specified(Scheme givenScheme)
        {
            var endpoint = Endpoint.Create("localhost", 5672, "guest", "guest", givenScheme);
            Assert.Equal(givenScheme, endpoint.Scheme);
        }

        public static IEnumerable<object[]> SchemaData()
        {
            return new[]
            {
                new object[] { Scheme.Amqp },
                new object[] { Scheme.Amqps }
            };
        }

        [Fact]
        public void Throws_when_invalid_scheme_specified()
        {
            var exception = Assert.Throws<CreateEndpointException>(() => Endpoint.Create("localhost", 5672, "guest", "guest", (Scheme) 999));
            Assert.Equal(ErrorCode.InvalidField, exception.ErrorCode);
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
                new object[] { Endpoint.Create("localhost", 5762), "amqp://localhost:5762" },
                new object[] { Endpoint.Create("localhost", 5762, scheme: Scheme.Amqps), "amqps://localhost:5762" },
                new object[] { Endpoint.Create("localhost", 5762, "admin", password: "secret"), "amqp://localhost:5762" },
                new object[] { Endpoint.Create("localhost", 5762, "admin", password: "secret", Scheme.Amqps), "amqps://localhost:5762" }
            };
        }
    }
}