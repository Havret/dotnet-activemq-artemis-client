using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace ActiveMQ.Artemis.Client.Chaos;

/// <summary>
/// Executes operations of the Artemis ActiveMQServerControl MBean through Jolokia.
/// </summary>
public sealed class JolokiaClient : IDisposable
{
    private readonly HttpClient _http;
    private readonly string _mbean;

    public JolokiaClient(Uri consoleUri, string user, string password, string brokerName)
    {
        _http = new HttpClient { BaseAddress = consoleUri, Timeout = TimeSpan.FromSeconds(10) };
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user}:{password}")));
        // Jolokia's CORS checks reject requests without a matching origin
        _http.DefaultRequestHeaders.Add("Origin", "http://localhost");
        _mbean = $"org.apache.activemq.artemis:broker=\"{brokerName}\"";
    }

    public async Task<JsonElement> ExecAsync(string operation, object[] arguments, CancellationToken ct)
    {
        var response = await _http.PostAsJsonAsync("console/jolokia/", new { type = "exec", mbean = _mbean, operation, arguments }, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Jolokia {operation} failed with {(int) response.StatusCode}: {body}");
        }

        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        if (root.TryGetProperty("status", out var status) && status.GetInt32() != 200)
        {
            throw new InvalidOperationException($"Jolokia {operation} failed: {(root.TryGetProperty("error", out var error) ? error.GetString() : body)}");
        }

        return root.TryGetProperty("value", out var value) ? value.Clone() : default;
    }

    public void Dispose() => _http.Dispose();
}
