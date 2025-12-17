using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Monody.AI.Tools.Capabilities.Geocode;

internal sealed class GeocodeToolRequest
{
    [Description("Free-form location query such as a city name, ZIP/postal code, or place name (e.g., 'Raleigh NC', '90210').")]
    [Required]
    public string Location { get; set; }
}
