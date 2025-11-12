using System;
using DarkSky.Models;

namespace Monody.Module.Weather.Models;

public class ForecastDay
{
    public DateTimeOffset Date { get; set; }
    public double High { get; set; }
    public double Low { get; set; }
    public string Summary { get; set; }
    public Icon Icon { get; set; }
}
