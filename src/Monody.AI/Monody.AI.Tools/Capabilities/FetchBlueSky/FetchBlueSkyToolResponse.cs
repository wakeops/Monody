using System.ComponentModel;

namespace Monody.AI.Tools.Capabilities.FetchBlueSky;

public sealed class FetchBlueSkyToolResponse
{
    [Description("Processed response data as text.")]
    public string Content { get; set; } = default!;
}
