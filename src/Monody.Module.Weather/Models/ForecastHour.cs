using System;
using DarkSky.Models;

namespace Monody.Module.Weather.Models;

public class ForecastHour
{
    public DateTimeOffset Date { get; set; }
    public double Temperature { get; set; }
    public double FeelsLikeTemperature { get; set; }
    public double PrecipitationProbability { get; set; }
    public double PrecipitationIntensity { get; set; }
    public double CloudCover { get; set; }
    public double Humidity { get; set; }
    public double WindSpeed { get; set; }
    public int? WindBearing { get; set; }
    public Icon Icon { get; set; }
    public string Summary { get; set; }
}
