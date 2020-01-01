using System;
using System.Threading.Tasks;

namespace ActiveMQ.Net
{
    public interface IConsumer : IAsyncDisposable
    {
        ValueTask<Message> ConsumeAsync();
        void Accept(Message message);
    }
}