﻿using System;
using Amqp.Listener;

namespace ActiveMQ.Artemis.Client.UnitTests.Utils
{
    public class TestLinkProcessor : ILinkProcessor
    {
        private Func<AttachContext, bool> _attachHandler;

        public void SetHandler(Func<AttachContext, bool> attachHandler)
        {
            _attachHandler = attachHandler;
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