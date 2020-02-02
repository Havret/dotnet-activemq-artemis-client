using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ActiveMQ.Net
{
    public static class ConnectionFactoryExtensions
    {
        public static Task<IConnection> CreateAsync(this ConnectionFactory connectionFactory, IEnumerable<Endpoint> endpoints)
        {
            return connectionFactory.CreateAsync(endpoints, CancellationToken.None);
        }
        
        public static Task<IConnection> CreateAsync(this ConnectionFactory connectionFactory, Endpoint endpoint, CancellationToken cancellationToken)
        {
            return connectionFactory.CreateAsync(new[] { endpoint }, cancellationToken);
        }
        
        public static Task<IConnection> CreateAsync(this ConnectionFactory connectionFactory, Endpoint endpoint)
        {
            return connectionFactory.CreateAsync(new[] { endpoint }, CancellationToken.None);
        }
    }
}