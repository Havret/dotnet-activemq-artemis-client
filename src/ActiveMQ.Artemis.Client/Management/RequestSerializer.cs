using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ActiveMQ.Artemis.Client.Management
{
    internal static class RequestSerializer
    {
        public static string CreateAddressToJson(string name, IEnumerable<RoutingType> routingTypes)
        {
            return JsonSerializer.Serialize(new[]
            {
                name,
                string.Join(",", routingTypes.Select(x => x.ToString().ToUpper()))
            });
        }
        
        public static async ValueTask<string> CreateQueueToJson(QueueConfiguration configuration)
        {
            using var stream = new MemoryStream();
            await using (var writer = new Utf8JsonWriter(stream))
            {
                writer.WriteStartObject();
                writer.WriteString("name", configuration.Name);
                writer.WriteString("address", configuration.Address);
                writer.WriteString("routing-type", configuration.RoutingType.ToString().ToUpper());
                writer.WriteBoolean("durable", configuration.Durable);
                writer.WriteNumber("max-consumers", configuration.MaxConsumers);
                writer.WriteBoolean("exclusive", configuration.Exclusive);
                writer.WriteBoolean("group-rebalance", configuration.GroupRebalance);
                writer.WriteNumber("group-buckets", configuration.GroupBuckets);
                writer.WriteBoolean("purge-on-no-consumers", configuration.PurgeOnNoConsumers);
                writer.WriteBoolean("auto-create-address", configuration.AutoCreateAddress);
                writer.WriteEndObject();
            }

            var json = Encoding.UTF8.GetString(stream.ToArray());
            return JsonSerializer.Serialize(new[] { json });
        }
    }
}