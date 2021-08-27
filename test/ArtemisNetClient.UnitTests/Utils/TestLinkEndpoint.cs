using Amqp.Listener;

namespace ActiveMQ.Artemis.Client.UnitTests.Utils
{
    public class TestLinkEndpoint : LinkEndpoint
    {
        public override void OnMessage(MessageContext messageContext)
        {
            messageContext.Complete();
        }

        public override void OnFlow(FlowContext flowContext)
        {
        }

        public override void OnDisposition(DispositionContext dispositionContext)
        {
            dispositionContext.Complete();
        }
    }
}