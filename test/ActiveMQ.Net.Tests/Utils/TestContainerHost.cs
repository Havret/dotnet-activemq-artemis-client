using System;
using System.Collections.Generic;
using Amqp.Handler;
using Amqp.Listener;

namespace ActiveMQ.Net.Tests.Utils
{
    public class TestContainerHost : IDisposable
    {
        private readonly ContainerHost _host;

        public TestContainerHost(string address, IHandler handler = null)
        {
            var uri = new Uri(address);
            _host = new ContainerHost(new List<Uri> { uri }, null, uri.UserInfo);
            _host.Listeners[0].HandlerFactory = listener => handler;
            _host.RegisterLinkProcessor(new TestLinkProcessor());
        }

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
    }
}