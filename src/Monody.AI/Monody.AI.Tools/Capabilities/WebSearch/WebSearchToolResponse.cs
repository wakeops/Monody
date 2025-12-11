using System.ComponentModel;

namespace Monody.AI.Tools.Capabilities.WebSearch;

internal sealed class WebSearchToolResponse
{
    [Description("Query results.")]
    public string Results { get; set; } = default!;
}
