using System.ComponentModel.DataAnnotations;

namespace Monody.Services.Geocode;

internal sealed class GeocodeOptions
{
    [Required(AllowEmptyStrings = false)]
    public string HereApiKey { get; set; } = string.Empty;
}
