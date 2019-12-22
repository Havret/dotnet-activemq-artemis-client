using System.Threading.Tasks;

namespace ActiveMQ.Net
{
    public interface IConsumer
    {
        ValueTask<Message> ConsumeAsync();
    }
}