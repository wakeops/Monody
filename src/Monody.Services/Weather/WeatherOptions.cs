using System.ComponentModel.DataAnnotations;

namespace Monody.Services.Weather;

internal sealed class WeatherOptions
{
    [Required(AllowEmptyStrings = false)]
    public string PirateWeatherApiKey { get; set; } = string.Empty;
}
