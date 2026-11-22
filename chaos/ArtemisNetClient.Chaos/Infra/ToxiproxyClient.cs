using System.Net;
using System.Net.Http.Json;

namespace ActiveMQ.Artemis.Client.Chaos;

/// <summary>
/// Minimal client of the Toxiproxy HTTP API (https://github.com/Shopify/toxiproxy#http-api).
/// </summary>
public sealed class ToxiproxyClient : IDisposable
{
    private readonly HttpClient _http;

    public ToxiproxyClient(Uri apiUri)
    {
        _http = new HttpClient { BaseAddress = apiUri, Timeout = TimeSpan.FromSeconds(10) };
    }

    public async Task<string> GetVersionAsync(CancellationToken ct)
    {
        return await _http.GetStringAsync("version", ct);
    }

    public async Task RecreateProxyAsync(string name, string listen, string upstream, CancellationToken ct)
    {
        await DeleteIgnoringNotFound($"proxies/{name}", ct);
        var response = await _http.PostAsJsonAsync("proxies", new { name, listen, upstream, enabled = true }, ct);
        await EnsureSuccess(response);
    }

    public async Task SetEnabledAsync(string proxy, bool enabled, CancellationToken ct)
    {
        var response = await _http.PostAsJsonAsync($"proxies/{proxy}", new { enabled }, ct);
        await EnsureSuccess(response);
    }

    public async Task AddToxicAsync(string proxy, string name, string type, string stream, object attributes, CancellationToken ct)
    {
        var response = await _http.PostAsJsonAsync($"proxies/{proxy}/toxics", new { name, type, stream, toxicity = 1.0, attributes }, ct);
        await EnsureSuccess(response);
    }

    public Task RemoveToxicAsync(string proxy, string name, CancellationToken ct)
    {
        return DeleteIgnoringNotFound($"proxies/{proxy}/toxics/{name}", ct);
    }

    /// <summary>Enables all proxies and removes all toxics.</summary>
    public async Task ResetAsync(CancellationToken ct)
    {
        var response = await _http.PostAsync("reset", null, ct);
        await EnsureSuccess(response);
    }

    private async Task DeleteIgnoringNotFound(string uri, CancellationToken ct)
    {
        var response = await _http.DeleteAsync(uri, ct);
        if (response.StatusCode != HttpStatusCode.NotFound)
        {
            await EnsureSuccess(response);
        }
    }

    private static async Task EnsureSuccess(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Toxiproxy {response.RequestMessage?.Method} {response.RequestMessage?.RequestUri} failed with {(int) response.StatusCode}: {body}");
        }
    }

    public void Dispose() => _http.Dispose();
}
