using System.ComponentModel.DataAnnotations;

namespace Monody.Services.WebSearch;

public class WebSearchOptions
{
    [Required]
    public string GoogleApiKey { get; set; }

    [Required]
    public string GoogleSearchEngineId { get; set; }
}
