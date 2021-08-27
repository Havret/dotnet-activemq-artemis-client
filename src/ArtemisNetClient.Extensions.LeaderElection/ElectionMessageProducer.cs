using System.Threading;

namespace ActiveMQ.Artemis.Client.Extensions.LeaderElection
{
    internal class ElectionMessageProducer
    {
        private readonly Message _electionMessage = new Message("election_msg") { GroupId = "election_msg" };
        
        private readonly IProducer _producer;
        
        public ElectionMessageProducer(IProducer producer) => _producer = producer;
        public void SendElectionMessage(CancellationToken cancellationToken) => _producer.Send(_electionMessage, cancellationToken);
    }
}