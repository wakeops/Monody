using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Monody.Services.Weather.Models;

namespace Monody.AI.Tools.Capabilities.Weather;

public sealed class WeatherToolRequest
{
    [Description("Free-form location like 'Raleigh, NC' or '90210'. Use this OR lat/lon.")]
    public string LocationQuery { get; set; }

    [Description("Latitude in decimal degrees. Use with longitude, instead of location_query.")]
    [Range(-90, 90)]
    public double? Latitude { get; set; }

    [Description("Longitude in decimal degrees. Use with latitude, instead of location_query.")]
    [Range(-180, 180)]
    public double? Longitude { get; set; }

    [Description("The time range of weather data to return.")]
    [DefaultValue(WeatherRange.Current)]
    public WeatherRange Range { get; set; }

    [Description("Number of days to return for daily forecasts. Only applicable when range is 'daily'.")]
    [Range(1, 14)]
    public int? Days { get; set; }

    [Description("Units for temperature.")]
    [DefaultValue(MeasurementUnits.Imperial)]
    public MeasurementUnits Units { get; set; }
}
