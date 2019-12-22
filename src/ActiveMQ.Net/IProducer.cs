using System.Threading.Tasks;

namespace ActiveMQ.Net
{
    public interface IProducer
    {
        Task ProduceAsync(Message message);
    }
}