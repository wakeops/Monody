using System.ComponentModel;
using Monody.Services.Geocode.Models;

namespace Monody.AI.Tools.Capabilities.Weather;

internal sealed class WeatherToolResponse
{
    [Description("The JSON geocode if latitude and longitude weren't provided.")]
    public LocationDetails GeocodeData { get; set; }

    [Description("The JSON weather data for the location.")]
    public object WeatherData { get; set; }
}
