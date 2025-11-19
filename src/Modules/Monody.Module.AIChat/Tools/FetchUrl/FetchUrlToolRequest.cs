using System.Text.Json.Serialization;

namespace Monody.Module.AIChat.Tools.FetchUrl;

internal class FetchUrlToolRequest
{
    [JsonPropertyName("url")]
    public string Url { get; set; }
}
