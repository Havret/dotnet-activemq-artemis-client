using System;
using Amqp.Listener;

namespace ActiveMQ.Net.Tests.Utils
{
    public class TestLinkProcessor : ILinkProcessor
    {
        private Func<AttachContext, bool> _attachHandler;

        public void SetHandler(Func<AttachContext, bool> attachHandler)
        {
            this._attachHandler = attachHandler;
        }

        public void Process(AttachContext attachContext)
        {
            if (_attachHandler != null)
            {
                if (_attachHandler(attachContext))
                {
                    return;
                }
            }

            attachContext.Complete(new TestLinkEndpoint(), attachContext.Attach.Role ? 0 : 30);
        }
    }
}