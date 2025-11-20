using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Monody.Module.AIChat.Tools.Definitions.FetchUrl;

internal class FetchUrlToolRequest
{
    [JsonPropertyName("url")]
    [Description("The full URL to fetch (http or https).")]
    [Required]
    public string Url { get; set; }
}
