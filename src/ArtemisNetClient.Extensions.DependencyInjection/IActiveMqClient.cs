using System;
using System.Threading;
using System.Threading.Tasks;

namespace ActiveMQ.Artemis.Client.Extensions.DependencyInjection
{
    public interface IActiveMqClient
    {
        Task StartAsync(CancellationToken cancellationToken);
        Task StopAsync(CancellationToken cancellationToken);

        event EventHandler<ConsumerErrorEventArgs> ConsumerError;
        event EventHandler<ProducerErrorEventArgs> ProducerError;
    }

    public class ConsumerErrorEventArgs : EventArgs
    {
        public ConsumerErrorEventArgs(Exception exception)
        {
            Exception = exception;
        }

        public Exception Exception { get; }
    }

    public class ProducerErrorEventArgs : EventArgs
    {
        public ProducerErrorEventArgs(Exception exception)
        {
            Exception = exception;
        }

        public Exception Exception { get; }
    }
}