using System.ComponentModel;

namespace Monody.AI.Tools.Capabilities.FetchUrl;

public sealed class FetchUrlToolResponse
{
    [Description("HTTP status code returned by the server.")]
    public int StatusCode { get; set; }

    [Description("Raw response body as text.")]
    public string Body { get; set; } = default!;
}
