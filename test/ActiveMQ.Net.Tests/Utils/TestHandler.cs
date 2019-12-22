using System;
using Amqp.Handler;

namespace ActiveMQ.Net.Tests.Utils
{
    public class TestHandler : IHandler
    {
        readonly Action<Event> _action;

        public TestHandler(Action<Event> action)
        {
            this._action = action;
        }

        bool IHandler.CanHandle(EventId id)
        {
            return true;
        }

        void IHandler.Handle(Event protocolEvent)
        {
            this._action(protocolEvent);
        }
    }
}