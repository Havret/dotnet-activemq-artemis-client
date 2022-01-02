using ActiveMQ.Artemis.Client.Testing.Listener;
using Amqp.Listener;
using Amqp.Transactions;

namespace ActiveMQ.Artemis.Client.Testing;

internal class TransactionProcessor : IMessageProcessor
{
    public void Process(MessageContext messageContext)
    {
        if (messageContext.Message.Body is Declare)
        {
            messageContext.Dispose(new Declared
            {
                TxnId = Guid.NewGuid().ToByteArray()
            });
        }
        else
        {
            messageContext.Complete();
        }
    }

    public int Credit => 30;
}