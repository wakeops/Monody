using System.ComponentModel.DataAnnotations;

namespace Monody.Module.Weather;

public sealed class WeatherOptions
{
    [Required(AllowEmptyStrings = false, ErrorMessage = "Weather:HereApiKey is required")]
    public string HereApiKey { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false, ErrorMessage = "Weather:PirateWeatherApiKey is required")]
    public string PirateWeatherApiKey { get; set; } = string.Empty;
}
