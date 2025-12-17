using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Monody.AI.Tools.Attributes;
using Monody.Services.Weather.Models;

namespace Monody.AI.Tools.Capabilities.Weather;

public sealed class WeatherToolRequest
{
    [Description("Free-form location query such as a city name, ZIP/postal code, or place name (e.g., 'Raleigh NC', '90210').")]
    [OneOfRequired(1)]
    public string LocationQuery { get; set; }

    [Description("Latitude in decimal degrees.")]
    [OneOfRequired(2)]
    [Range(-90, 90)]
    public double? Latitude { get; set; }

    [Description("Longitude in decimal degrees.")]
    [OneOfRequired(2)]
    [Range(-180, 180)]
    public double? Longitude { get; set; }

    [Description("The time range of weather data to return.")]
    [DefaultValue(WeatherRange.Current)]
    public WeatherRange Range { get; set; }

    [Description("Number of days to return for daily forecasts. Only applicable when range is 'daily'.")]
    [Range(1, 14)]
    public int? Days { get; set; }

    [Description("Units for temperature and wind speed.")]
    [DefaultValue(MeasurementUnits.Imperial)]
    public MeasurementUnits Units { get; set; }
}
