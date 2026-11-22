using System;

namespace ActiveMQ.Artemis.Client.Exceptions
{
    /// <summary>
    /// Thrown when a message can't be accepted, rejected or modified, because the link it was received on is gone,
    /// e.g. the connection was lost in the meantime. Unlike <see cref="ConsumerClosedException"/>, it doesn't mean that
    /// the consumer is closed. It's going to be recovered and to receive messages again. The message stays unsettled,
    /// so the broker may redeliver it.
    /// </summary>
    public class MessageSettlementException : ActiveMQArtemisClientException
    {
        private const string DefaultMessage = "The message can't be settled, because the link it was received on was closed. The broker may redeliver it.";

        public MessageSettlementException() : base(DefaultMessage)
        {
        }

        public MessageSettlementException(Exception innerException) : base(DefaultMessage, innerException)
        {
        }

        public MessageSettlementException(string message) : base(message)
        {
        }
    }
}
