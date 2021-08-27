using Microsoft.Extensions.DependencyInjection;

namespace ActiveMQ.Artemis.Client.Extensions.DependencyInjection
{
    public interface IActiveMqBuilder
    {
        /// <summary>
        /// Gets the name of the connection configured by this builder.
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// Gets the application service collection.
        /// </summary>
        IServiceCollection Services { get; }
    }
}