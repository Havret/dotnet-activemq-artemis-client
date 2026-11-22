using System.Text.Json;
using System.Text.Json.Serialization;

namespace ActiveMQ.Artemis.Client.Chaos;

public static class Json
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public static readonly JsonSerializerOptions Indented = new(Options) { WriteIndented = true };
}
