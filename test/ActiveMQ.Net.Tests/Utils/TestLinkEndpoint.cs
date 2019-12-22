using Amqp.Listener;

namespace ActiveMQ.Net.Tests.Utils
{
    public class TestLinkEndpoint : LinkEndpoint
    {
        public override void OnMessage(MessageContext messageContext)
        {
            messageContext.Complete();
        }

        public override void OnFlow(FlowContext flowContext)
        {
            for (int i = 0; i < flowContext.Messages; i++)
            {
                var message = new Amqp.Message("test message");
                flowContext.Link.SendMessage(message, message.Encode());
            }
        }

        public override void OnDisposition(DispositionContext dispositionContext)
        {
            dispositionContext.Complete();
        }
    }
}