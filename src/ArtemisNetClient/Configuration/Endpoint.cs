using System;
using ActiveMQ.Artemis.Client.Exceptions;
using Amqp;

namespace ActiveMQ.Artemis.Client
{
    public sealed class Endpoint
    {
        private const string Amqp = "AMQP";
        private const string Amqps = "AMQPS";
        private const string Ws = "WS";
        private const string Wss = "WSS";

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

        /// <summary>
        /// Gets the path of the endpoint.
        /// </summary>
        public string Path => Address.Path;

        public static Endpoint Create(string host, int port, string user = null, string password = null, Scheme scheme = Scheme.Amqp, string path = "/")
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
                throw new CreateEndpointException(e.Error.Description, e.Error.Condition, e);
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
                Scheme.Ws => Ws,
                Scheme.Wss => Wss,
                _ => throw new CreateEndpointException($"Protocol scheme {scheme.ToString()} is invalid.", ErrorCode.InvalidField)
            };
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $@"{Scheme.ToString().ToLower()}://{Host}:{Port.ToString()}{Path}";
        }
    }
}