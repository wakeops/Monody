using System.ComponentModel;

namespace Monody.AI.Tools.Capabilities.Geocode;

internal sealed class GeocodeToolResponse
{
    [Description("The JSON geocode of the location")]
    public string Response { get; set; }
}
