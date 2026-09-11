using System.Text.Json.Serialization;

namespace Monody.Services.Graylog.Models;

internal sealed class GraylogStreamsResponse
{
    [JsonPropertyName("streams")]
    public List<GraylogStreamDto> Streams { get; set; } = [];
}

internal sealed class GraylogStreamDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("disabled")]
    public bool Disabled { get; set; }
}
