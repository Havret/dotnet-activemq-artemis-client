using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ActiveMQ.Artemis.Client
{
    internal static class RequestSerializer
    {
        public static string AddressInfoToJson(string name, IEnumerable<RoutingType> routingTypes)
        {
            return JsonSerializer.Serialize(new[]
            {
                name,
                string.Join(",", routingTypes.Select(x => x.ToString().ToUpper()))
            });
        }
        
        public static async ValueTask<string> QueueConfigurationToJson(QueueConfiguration configuration)
        {
            using var stream = new MemoryStream();
            var writer = new Utf8JsonWriter(stream);
            await using (writer.ConfigureAwait(false))
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
                writer.WriteString("filter-string", configuration.FilterExpression ?? string.Empty);
                writer.WriteBoolean("auto-delete", configuration.AutoDelete);
                writer.WriteNumber("auto-delete-message-count", configuration.AutoDeleteMessageCount);
                if (configuration.AutoDeleteDelay.HasValue)
                {
                    writer.WriteNumber("auto-delete-delay", Convert.ToInt64(configuration.AutoDeleteDelay.Value.TotalMilliseconds));    
                }
                else
                {
                    writer.WriteNumber("auto-delete-delay", -1);
                }
                if (string.IsNullOrEmpty(configuration.LastValueKey) == false)
                {
                    writer.WriteString("last-value-key", configuration.LastValueKey);
                }
                writer.WriteEndObject();
            }

            var json = Encoding.UTF8.GetString(stream.ToArray());
            return JsonSerializer.Serialize(new[] { json });
        }
    }
}