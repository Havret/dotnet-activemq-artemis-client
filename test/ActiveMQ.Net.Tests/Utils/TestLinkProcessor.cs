using System;
using Amqp.Listener;

namespace ActiveMQ.Net.Tests.Utils
{
    public class TestLinkProcessor : ILinkProcessor
    {
        private Func<AttachContext, bool> _attachHandler;
        private readonly Func<ListenerLink, LinkEndpoint> _factory;

        public TestLinkProcessor()
        {
        }

        public TestLinkProcessor(Func<ListenerLink, LinkEndpoint> factory)
        {
            this._factory = factory;
        }

        public void SetHandler(Func<AttachContext, bool> attachHandler)
        {
            this._attachHandler = attachHandler;
        }

        public void Process(AttachContext attachContext)
        {
            if (this._attachHandler != null)
            {
                if (this._attachHandler(attachContext))
                {
                    return;
                }
            }

            attachContext.Complete(
                this._factory != null ? this._factory(attachContext.Link) : new TestLinkEndpoint(),
                attachContext.Attach.Role ? 0 : 30);
        }
    }
}