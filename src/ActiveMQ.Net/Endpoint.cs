using System;
using Amqp;

namespace ActiveMQ.Net
{
    public sealed class Endpoint
    {
        private const string Amqp = "AMQP";
        private const string Amqps = "AMQPS";

        private Endpoint(Address address)
        {
            Address = address;
        }

        internal Address Address { get; }

        /// <summary>
        /// Gets the protocol scheme.
        /// </summary>
        public Scheme Scheme { get; private set; }

        /// <summary>
        /// Gets the host of the endpoint.
        /// </summary>
        public string Host => Address.Host;

        /// <summary>
        /// Gets the port number of the endpoint.
        /// </summary>
        public int Port => Address.Port;

        /// <summary>
        /// Gets the user name that is used for SASL PLAIN profile.
        /// </summary>
        public string User => Address.User;

        /// <summary>
        /// Gets the password that is used for SASL PLAIN profile.
        /// </summary>
        public string Password => Address.Password;

        public static Endpoint Create(string host, int port, string user = null, string password = null, Scheme scheme = Scheme.Amqp)
        {
            return Create(host, port, user, password, "/", scheme);
        }

        public static Endpoint Create(string host, int port, string user, string password, string path, Scheme scheme)
        {
            var protocolScheme = GetScheme(scheme);

            try
            {
                return new Endpoint(new Address(host, port, user, password, path, protocolScheme))
                {
                    Scheme = scheme
                };
            }
            catch (AmqpException e)
            {
                throw CreateEndpointException.FromError(e.Error);
            }
            catch (Exception e)
            {
                throw new CreateEndpointException("Could not create endpoint", e);
            }
        }

        private static string GetScheme(Scheme scheme)
        {
            return scheme switch
            {
                Scheme.Amqp => Amqp,
                Scheme.Amqps => Amqps,
                _ => throw new CreateEndpointException(ErrorCode.InvalidField, $"Protocol scheme {scheme.ToString()} is invalid.")
            };
        }
    }
}