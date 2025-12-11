using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Monody.AI.Tools.Capabilities.WebSearch;

internal sealed class WebSearchToolRequest
{
    [Description("The search terms to look up.")]
    [Required]
    public string Query { get; set; }
}
