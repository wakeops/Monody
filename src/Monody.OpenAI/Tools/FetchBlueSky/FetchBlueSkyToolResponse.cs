using System.ComponentModel;

namespace Monody.OpenAI.Tools.FetchBlueSky;

internal sealed class FetchBlueSkyToolResponse
{
    [Description("Processed response data as text.")]
    public string Content { get; set; } = default!;
}
