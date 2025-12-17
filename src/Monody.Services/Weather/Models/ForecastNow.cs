using System.Collections.Generic;
using DarkSky.Models;

namespace Monody.Services.Weather.Models;

public class ForecastNow
{
    public string Condition { get; set; }
    public double Temperature { get; set; }
    public double Humidity { get; set; }
    public double WindChill { get; set; }
    public double WindSpeed { get; set; }
    public double WindGust { get; set; }
    public int WindBearing { get; set; }
    public string CardinalWindBearing { get; set; }
    public double ForecastHigh { get; set; }
    public double ForecastLow { get; set;}
    public double HeatIndex { get; set; }
    public Icon Icon { get; set; }
    public int UVIndex { get; set; }
    public double PrecipitationProbability { get; set; }
    public PrecipitationType PrecipitationType { get; set; }
    public double PrecipitationIntensity { get; set; }
    public double? PrecipitationIntensityMax { get; set; }
    public double SnowAccumulation { get; set; }
    public IEnumerable<WeatherAlert> Alerts { get; set; }
}
