using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Monody.Services.Graylog.Models;

namespace Monody.Services.Graylog;

public class GraylogService
{
    private readonly HttpClient _httpClient;
    private readonly GraylogOptions _options;

    public GraylogService(HttpClient httpClient, IOptions<GraylogOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    /// <summary>Whether enough configuration is present to attempt a call at all.</summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_options.BaseUrl) && !string.IsNullOrWhiteSpace(_options.ApiKey);

    public async Task<GraylogSearchResult> SearchAsync(string query, int rangeSeconds, int limit, string streamId, CancellationToken cancellationToken = default)
    {
        var url = $"search/universal/relative?query={Uri.EscapeDataString(query)}&range={rangeSeconds}&limit={limit}&sort=timestamp:desc";

        if (!string.IsNullOrWhiteSpace(streamId))
        {
            url += $"&filter={Uri.EscapeDataString($"streams:{streamId}")}";
        }

        var json = await GetJsonAsync(url, cancellationToken);

        var parsed = JsonSerializer.Deserialize<GraylogSearchResponse>(json)
            ?? throw new InvalidOperationException("Failed to deserialize the Graylog search response.");

        return new GraylogSearchResult(parsed.TotalResults, [.. parsed.Messages.Select(FormatMessage)]);
    }

    /// <summary>Enabled streams only - a disabled stream can't be searched, so there's no reason to offer one.</summary>
    public async Task<IReadOnlyList<GraylogStream>> GetStreamsAsync(CancellationToken cancellationToken = default)
    {
        var json = await GetJsonAsync("streams", cancellationToken);

        var parsed = JsonSerializer.Deserialize<GraylogStreamsResponse>(json)
            ?? throw new InvalidOperationException("Failed to deserialize the Graylog streams response.");

        return [.. parsed.Streams
            .Where(s => !s.Disabled)
            .Select(s => new GraylogStream(s.Id, s.Title, s.Description))];
    }

    private async Task<string> GetJsonAsync(string url, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);

        // Graylog API tokens authenticate as HTTP Basic credentials: the token as the username,
        // and the literal string "token" as the password.
        var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_options.ApiKey}:token"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    private static string FormatMessage(GraylogMessageWrapper wrapper)
    {
        var fields = wrapper.Fields;

        var timestamp = FieldOrDefault(fields, "timestamp");
        var source = FieldOrDefault(fields, "source");
        var message = FieldOrDefault(fields, "message");

        return $"[{timestamp}] {source}: {message}";
    }

    private static string FieldOrDefault(Dictionary<string, JsonElement> fields, string name) =>
        fields.TryGetValue(name, out var value) ? value.ToString() : "";
}
