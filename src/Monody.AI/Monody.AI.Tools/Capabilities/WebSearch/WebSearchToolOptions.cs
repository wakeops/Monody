using System.ComponentModel.DataAnnotations;

namespace Monody.AI.Tools.Capabilities.WebSearch;

internal class WebSearchToolOptions
{
    [Required]
    public string GoogleApiKey { get; set; }

    [Required]
    public string GoogleSearchEngineId { get; set; }
}
