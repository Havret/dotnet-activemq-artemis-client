using System;
using Amqp.Handler;
using Amqp.Listener;

namespace ActiveMQ.Artemis.Client.UnitTests.Utils
{
    public class TestContainerHost : IDisposable
    {
        private readonly ContainerHost _host;
        private readonly TestLinkProcessor _linkProcessor;

        public TestContainerHost(Endpoint endpoint, IHandler handler = null)
        {
            Endpoint = endpoint;            
            _host = new ContainerHost(endpoint.Address);
            _host.Listeners[0].HandlerFactory = listener => handler;
            _linkProcessor = new TestLinkProcessor();
            _host.RegisterLinkProcessor(_linkProcessor);
        }
        
        public Endpoint Endpoint { get; }

        public void Dispose() => _host.Close();
        public void Open() => _host.Open();
        
        public MessageSource CreateMessageSource(string address)
        {
            var messageSource = new MessageSource();
            _host.RegisterMessageSource(address, messageSource);
            return messageSource;
        }

        public MessageProcessor CreateMessageProcessor(string address)
        {
            var messageProcessor = new MessageProcessor();
            _host.RegisterMessageProcessor(address, messageProcessor);
            return messageProcessor;
        }

        public TestLinkProcessor CreateTestLinkProcessor()
        {
            return _linkProcessor;
        }
    }
}