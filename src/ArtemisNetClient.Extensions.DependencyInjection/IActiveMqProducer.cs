using System;
using System.Threading;
using System.Threading.Tasks;

namespace ActiveMQ.Artemis.Client.Extensions.DependencyInjection
{
    internal interface IActiveMqProducer
    {
        ValueTask StartAsync(CancellationToken cancellationToken, Action<Exception> producerException);
        ValueTask StopAsync();
    }
}