using System.ComponentModel;

namespace Monody.AI.Tools.Capabilities.WebSearch;

public sealed class WebSearchToolResponse
{
    [Description("Query results.")]
    public string Results { get; set; } = default!;
}
