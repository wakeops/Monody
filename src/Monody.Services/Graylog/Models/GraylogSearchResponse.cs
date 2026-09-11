using System.Text.Json;
using System.Text.Json.Serialization;

namespace Monody.Services.Graylog.Models;

/// <summary>
/// Shape of Graylog's legacy universal search endpoint (GET /api/search/universal/relative),
/// which is what this targets on Graylog 4.3.15 - the newer Views/search-jobs API requires an
/// asynchronous job/poll/result flow that isn't worth the extra round trips for a single query.
/// </summary>
internal sealed class GraylogSearchResponse
{
    [JsonPropertyName("total_results")]
    public int TotalResults { get; set; }

    [JsonPropertyName("messages")]
    public List<GraylogMessageWrapper> Messages { get; set; } = [];
}

internal sealed class GraylogMessageWrapper
{
    [JsonPropertyName("message")]
    public Dictionary<string, JsonElement> Fields { get; set; } = [];
}
