using System;
using Amqp.Handler;

namespace ActiveMQ.Artemis.Client.UnitTests.Utils
{
    public class TestHandler : IHandler
    {
        readonly Action<Event> _action;

        public TestHandler(Action<Event> action)
        {
            _action = action;
        }

        bool IHandler.CanHandle(EventId id)
        {
            return true;
        }

        void IHandler.Handle(Event protocolEvent)
        {
            _action(protocolEvent);
        }
    }
}